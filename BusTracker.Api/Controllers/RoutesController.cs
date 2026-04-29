using BusTracker.Api.Authorization;
using BusTracker.Application.Common.Auth;
using BusTracker.Application.Features.Routes.Commands.CreateRoute;
using BusTracker.Application.Features.Routes.Queries.GetRouteById;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BusTracker.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class RoutesController : ControllerBase
    {
        private readonly ISender _sender;

        public RoutesController(ISender sender)
        {
            _sender = sender;
        }

        [HttpPost]
        //[HasPermission(Permissions.Orgs.Update)]
        public async Task<IActionResult> Create([FromBody] CreateRouteCommand command)
        {
            var routeId = await _sender.Send(command);
            return CreatedAtAction(nameof(GetById), new { id = routeId }, new { Id = routeId });
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            return Ok(await _sender.Send(new GetRouteByIdQuery(id)));
        }
    }
}
