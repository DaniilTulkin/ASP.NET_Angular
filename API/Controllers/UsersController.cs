﻿using API.DTOs;
using API.Entities;
using API.Extensions;
using API.Helpers;
using API.Interfaces;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace API.Controllers
{
    [Authorize]
    public class UsersController : BaseApiController
    {
        private readonly IUnitOfWork unitOfWork;
        private readonly IMapper mapper;
        private readonly IPhotoService photoService;

        public UsersController(IUnitOfWork unitOfWork, 
                               IMapper mapper,
                               IPhotoService photoService) 
        {
            this.photoService = photoService;
            this.unitOfWork = unitOfWork;
            this.mapper = mapper;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<MemberDTO>>> GetUsersAsync(
            [FromQuery]UserParams userParams) 
        {
            var gender = await unitOfWork.UserRepository.GetUserGenderAsync(User.GetUserName());
            userParams.CurrentUserName = User.GetUserName();
            if (string.IsNullOrEmpty(userParams.Gender)) 
            {
                userParams.Gender = gender == "male" ? 
                    "female" : "male";
            }

            var users = await unitOfWork.UserRepository.GetMembersAsync(userParams);
            Response.AddPaginationHeader(users.CurrentPage, users.PageSize,
                users.TotalCount, users.TotalPages);

            return Ok(users);
        }

        [HttpGet("{userName}", Name = "GetUserAsync")]
        public async Task<ActionResult<MemberDTO>> GetUserAsync(string userName) =>
            await unitOfWork.UserRepository.GetMemberByNameAsync(userName);

        [HttpPut]
        public async Task<ActionResult> UpdateUserAsync(MemberUpdateDTO memberUpdateDTO) 
        {
            var user = await unitOfWork.UserRepository.GetUserByNameAsync(User.GetUserName());

            mapper.Map(memberUpdateDTO, user);
            unitOfWork.UserRepository.Update(user);

            if (await unitOfWork.CompleteAsync()) 
                return NoContent();
            return BadRequest("Failed to update user");
        }

        [HttpPost("add-photo")]
        public async Task<ActionResult<PhotoDTO>> AddPhotoAsync(IFormFile file) 
        {
            var user = await unitOfWork.UserRepository.GetUserByNameAsync(User.GetUserName());
            var result = await photoService.AddPhotoAsync(file);

            if (result.Error != null) return BadRequest(result.Error.Message);

            var photo = new Photo
            {
                Url = result.SecureUrl.AbsoluteUri,
                PublicId = result.PublicId
            };

            if (user.Photos.Count == 0) photo.IsMain = true;

            user.Photos.Add(photo);

            if (await unitOfWork.CompleteAsync())
            {
                return CreatedAtRoute("GetUserAsync", 
                                      new {UserName = user.UserName}, 
                                      mapper.Map<PhotoDTO>(photo));
            }

            return BadRequest("Adding photo error");
        }

        [HttpPut("set-main-photo/{photoId}")]
        public async Task<ActionResult> SetMainPhotoAsync(int photoId) 
        {
            var user = await unitOfWork.UserRepository.GetUserByNameAsync(User.GetUserName());
            var photo = user.Photos.FirstOrDefault(x => x.Id == photoId);
            if (photo.IsMain) return BadRequest("This is already your main photo");

            var currentMain = user.Photos.FirstOrDefault(x => x.IsMain);
            if (currentMain != null) currentMain.IsMain = false;
            photo.IsMain = true;

            if (await unitOfWork.CompleteAsync()) return NoContent();
            return BadRequest("Failed to set main photo");
        }

        [HttpDelete("delete-photo/{photoId}")]
        public async Task<ActionResult> DeletePhotoAsync(int photoId) 
        {
            var user = await unitOfWork.UserRepository.GetUserByNameAsync(User.GetUserName());
            var photo = user.Photos.FirstOrDefault(x => x.Id == photoId);

            if (photo == null) return NotFound();
            if (photo.IsMain) return BadRequest("You cannot delete your main photo");
            if (photo.PublicId != null) 
            {
                var result = await photoService.DeletePhotoAsync(photo.PublicId);
                if (result.Error != null) return BadRequest(result.Error.Message);
            }

            user.Photos.Remove(photo);

            if (await unitOfWork.CompleteAsync()) return Ok();
            return BadRequest("Failed to delete the photo");
        }
    }
}
