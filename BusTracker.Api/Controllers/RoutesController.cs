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
        [HasPermission(Permissions.Routes.Create)]
        public async Task<IActionResult> Create([FromBody] CreateRouteCommand command)
        {
            var routeId = await _sender.Send(command);
            return CreatedAtAction(nameof(GetById), new { id = routeId }, new { Id = routeId });
        }

        [HttpGet("search")]
        [AllowAnonymous] // Public search endpoint, though inner logic restricts private routes
        public async Task<IActionResult> Search([FromQuery] BusTracker.Application.Features.Routes.Queries.SearchRoutes.SearchRoutesQuery query)
        {
            return Ok(await _sender.Send(query));
        }

        [HttpGet("{id}")]
        [HasPermission(Permissions.Routes.Read)]
        public async Task<IActionResult> GetById(Guid id)
        {
            return Ok(await _sender.Send(new GetRouteByIdQuery(id)));
        }

        [HttpPut("{id}")]
        [HasPermission(Permissions.Routes.Update)]
        public async Task<IActionResult> Update(Guid id, [FromBody] BusTracker.Application.Features.Routes.Commands.UpdateRoute.UpdateRouteCommand command)
        {
            if (id != command.RouteId) return BadRequest("Path ID must match payload ID.");
            await _sender.Send(command);
            return NoContent();
        }
    }
}
