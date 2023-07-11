using System.Security.Claims;

namespace API.Extensions
{
    public static class ClaimsPrincipleExtension
    {
        public static string GetUserName(this ClaimsPrincipal user) => 
            user.FindFirst(ClaimTypes.Name)?.Value;

        public static int GetUserId(this ClaimsPrincipal user) => 
            int.Parse(user.FindFirst(ClaimTypes.NameIdentifier)?.Value);
    }
}