using API.Data;
using API.DTOs;
using API.Entities;
using API.Interfaces;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

namespace API.Controllers
{
    [Authorize]
    public class UsersController : BaseApiController
    {
        private readonly IUserRepository userRepository;
        private readonly IMapper mapper;

        public UsersController(IUserRepository userRepository, 
                               IMapper mapper) 
        {
            this.mapper = mapper;
            this.userRepository = userRepository;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<MemberDTO>>> GetUsersAsync() =>
            Ok(await userRepository.GetMembersAsync());

        [HttpGet("{userName}")]
        public async Task<ActionResult<MemberDTO>> GetUserAsync(string userName) =>
            await userRepository.GetMemberByNameAsync(userName);

        [HttpPut]
        public async Task<ActionResult> UpdateUserAsync(MemberUpdateDTO memberUpdateDTO) 
        {
            var userName = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var user = await userRepository.GetUserByNameAsync(userName);

            mapper.Map(memberUpdateDTO, user);
            userRepository.Update(user);

            if (await userRepository.SaveAllAsync()) 
                return NoContent();
            return BadRequest("Failed to update user");
        }
    }
}
