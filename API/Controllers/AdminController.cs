using System.Linq;
using System.Threading.Tasks;
using API.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Controllers
{
    public class AdminController : BaseApiController
    {
        private readonly UserManager<AppUser> userManger;
        public AdminController(UserManager<AppUser> userManger)
        {
            this.userManger = userManger;
        }

        [Authorize(Policy = "RequireAdminRole")]
        [HttpGet("users-with-roles")]
        public async Task<ActionResult> GetUsersWithRolseAsync() =>
            Ok(await userManger.Users
                .Include(r => r.UserRoles)
                .ThenInclude(r => r.Role)
                .OrderBy(u => u.UserName)
                .Select(u => new 
                {
                    u.Id,
                    UserName = u.UserName,
                    Roles = u.UserRoles.Select(r => r.Role.Name).ToList()
                })
                .ToListAsync());

        [HttpPost("edit-roles/{userName}")]
        public async Task<ActionResult> EditRolesAsync(string userName, 
            [FromQuery] string roles)
        {
            var selectedRoles = roles.Split(",").ToArray();
            var user = await userManger.FindByNameAsync(userName);
            if (user == null) return NotFound("Couldn't find user");
            var userRoles = await userManger.GetRolesAsync(user);

            var result = await userManger.AddToRolesAsync(
                user, selectedRoles.Except(userRoles));
            if (!result.Succeeded) return BadRequest(result.Errors);

            result = await userManger.RemoveFromRolesAsync(
                user, userRoles.Except(selectedRoles));
            if (!result.Succeeded) return BadRequest(result.Errors);

            return Ok(await userManger.GetRolesAsync(user));
        }

        [Authorize(Policy = "ModeratePhotosRole")]
        [HttpGet("photos-to-moderate")]
        public ActionResult GetPhotosForModeration() =>
            Ok("Only admins or moderators can see this");
    }
}