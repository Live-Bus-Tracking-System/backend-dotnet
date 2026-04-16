using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace BusTracker.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class testController : ControllerBase
    {
        [HttpGet]
        [HttpHead]
        public IActionResult Get()
        {
            return Ok("Hello, World!");
        }
    }
}
