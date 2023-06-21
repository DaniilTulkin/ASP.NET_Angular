using API.Data;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BaseApiController : ControllerBase
    {
        public readonly DataContext Context;

        public BaseApiController(DataContext context)
        {
            Context = context;
        }
    }
}
