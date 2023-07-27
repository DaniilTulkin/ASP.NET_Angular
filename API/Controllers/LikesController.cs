using System.Collections.Generic;
using System.Threading.Tasks;
using API.DTOs;
using API.Entities;
using API.Extensions;
using API.Helpers;
using API.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [Authorize]
    public class LikesController : BaseApiController
    {
        private readonly IUnitOfWork unitOfWork;

        public LikesController(IUnitOfWork unitOfWork)
        {
            this.unitOfWork = unitOfWork;
        }

        [HttpPost("{userName}")]
        public async Task<ActionResult> AddLikeAsync(string userName) 
        {
            var sourceUserId = User.GetUserId();
            var likedUser = await unitOfWork.UserRepository.GetUserByNameAsync(userName);
            var sourceUser = await unitOfWork.LikesRepository.GetUserWithLikesAsync(sourceUserId);

            if (likedUser == null) return NotFound();
            if (sourceUser.UserName == userName) return BadRequest("You cannot like yourself");

            var userLike = await unitOfWork.LikesRepository.GetUserLikeAsync(sourceUserId, likedUser.Id);
            if (userLike != null) return BadRequest("You have already liked this user");
            userLike = new UserLike 
            {
                SourceUserId = sourceUserId,
                LikedUserId = likedUser.Id
            };

            sourceUser.LikedUsers.Add(userLike);
            if (await unitOfWork.CompleteAsync()) return Ok();
            return BadRequest("Failed to like user");
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<LikeDTO>>> GetUserLikesAsync(
            [FromQuery]LikesParams likesParams) 
        {
            likesParams.UserId = User.GetUserId();
            var users = await unitOfWork.LikesRepository.GetUserLikesAsync(likesParams);

            Response.AddPaginationHeader(users.CurrentPage, users.PageSize,
                users.TotalCount, users.TotalPages);
            return Ok(users);   
        }
    }
}