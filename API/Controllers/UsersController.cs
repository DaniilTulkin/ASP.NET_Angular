using API.DTOs;
using API.Entities;
using API.Extensions;
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
        private readonly IUserRepository userRepository;
        private readonly IMapper mapper;
        private readonly IPhotoService photoService;

        public UsersController(IUserRepository userRepository, 
                               IMapper mapper,
                               IPhotoService photoService) 
        {
            this.photoService = photoService;
            this.mapper = mapper;
            this.userRepository = userRepository;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<MemberDTO>>> GetUsersAsync() =>
            Ok(await userRepository.GetMembersAsync());

        [HttpGet("{userName}", Name = "GetUserAsync")]
        public async Task<ActionResult<MemberDTO>> GetUserAsync(string userName) =>
            await userRepository.GetMemberByNameAsync(userName);

        [HttpPut]
        public async Task<ActionResult> UpdateUserAsync(MemberUpdateDTO memberUpdateDTO) 
        {
            var user = await userRepository.GetUserByNameAsync(User.GetUserName());

            mapper.Map(memberUpdateDTO, user);
            userRepository.Update(user);

            if (await userRepository.SaveAllAsync()) 
                return NoContent();
            return BadRequest("Failed to update user");
        }

        [HttpPost("add-photo")]
        public async Task<ActionResult<PhotoDTO>> AddPhotoAsync(IFormFile file) 
        {
            var user = await userRepository.GetUserByNameAsync(User.GetUserName());
            var result = await photoService.AddPhotoAsync(file);

            if (result.Error != null) return BadRequest(result.Error.Message);

            var photo = new Photo
            {
                Url = result.SecureUrl.AbsoluteUri,
                PublicId = result.PublicId
            };

            if (user.Photos.Count == 0) photo.IsMain = true;

            user.Photos.Add(photo);

            if (await userRepository.SaveAllAsync())
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
            var user = await userRepository.GetUserByNameAsync(User.GetUserName());
            var photo = user.Photos.FirstOrDefault(x => x.Id == photoId);
            if (photo.IsMain) return BadRequest("This is already your main photo");

            var currentMain = user.Photos.FirstOrDefault(x => x.IsMain);
            if (currentMain != null) currentMain.IsMain = false;
            photo.IsMain = true;

            if (await userRepository.SaveAllAsync()) return NoContent();
            return BadRequest("Failed to set main photo");
        }

        [HttpDelete("delete-photo/{photoId}")]
        public async Task<ActionResult> DeletePhotoAsync(int photoId) 
        {
            var user = await userRepository.GetUserByNameAsync(User.GetUserName());
            var photo = user.Photos.FirstOrDefault(x => x.Id == photoId);

            if (photo == null) return NotFound();
            if (photo.IsMain) return BadRequest("You cannot delete your main photo");
            if (photo.PublicId != null) 
            {
                var result = await photoService.DeletePhotoAsync(photo.PublicId);
                if (result.Error != null) return BadRequest(result.Error.Message);
            }

            user.Photos.Remove(photo);

            if (await userRepository.SaveAllAsync()) return Ok();
            return BadRequest("Failed to delete the photo");
        }
    }
}
