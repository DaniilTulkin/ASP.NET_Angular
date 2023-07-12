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
        private readonly IUserRepository userRepository;
        private readonly ILikesRepository likesRepository;
        
        public LikesController(IUserRepository userRepository,
            ILikesRepository likesRepository)
        {
            this.likesRepository = likesRepository;
            this.userRepository = userRepository;
        }

        [HttpPost("{userName}")]
        public async Task<ActionResult> AddLikeAsync(string userName) 
        {
            var sourceUserId = User.GetUserId();
            var likedUser = await userRepository.GetUserByNameAsync(userName);
            var sourceUser = await likesRepository.GetUserWithLikesAsync(sourceUserId);

            if (likedUser == null) return NotFound();
            if (sourceUser.UserName == userName) return BadRequest("You cannot like yourself");

            var userLike = await likesRepository.GetUserLikeAsync(sourceUserId, likedUser.Id);
            if (userLike != null) return BadRequest("You have already liked this user");
            userLike = new UserLike 
            {
                SourceUserId = sourceUserId,
                LikedUserId = likedUser.Id
            };

            sourceUser.LikedUsers.Add(userLike);
            if (await userRepository.SaveAllAsync()) return Ok();
            return BadRequest("Failed to like user");
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<LikeDTO>>> GetUserLikesAsync(
            [FromQuery]LikesParams likesParams) 
        {
            likesParams.UserId = User.GetUserId();
            var users = await likesRepository.GetUserLikesAsync(likesParams);

            Response.AddPaginationHeader(users.CurrentPage, users.PageSize,
                users.TotalCount, users.TotalPages);
            return Ok(users);   
        }
    }
}