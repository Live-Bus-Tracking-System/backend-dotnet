using BusTracker.Api.Authorization;
using BusTracker.Application.Common.Auth;
using BusTracker.Application.Features.Stops.Commands.CreateStop;
using BusTracker.Application.Features.Stops.Commands.DeleteStop;
using BusTracker.Application.Features.Stops.Commands.UpdateStop;
using BusTracker.Application.Features.Stops.Queries.GetStops;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BusTracker.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class StopsController : ControllerBase
    {
        private readonly ISender _sender;

        public StopsController(ISender sender)
        {
            _sender = sender;
        }

        [HttpPost]
        [HasPermission(Permissions.Orgs.Update)] // Adjust permission as needed
        public async Task<IActionResult> Create([FromBody] CreateStopCommand command)
        {
            var stopId = await _sender.Send(command);
            return CreatedAtAction(nameof(GetAll), new { id = stopId }, new { Id = stopId });
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            return Ok(await _sender.Send(new GetStopsQuery()));
        }

        [HttpPut("{id}")]
        [HasPermission(Permissions.Orgs.Update)]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateStopCommand command)
        {
            if (id != command.Id)
            {
                return BadRequest("ID in route must match ID in payload.");
            }

            await _sender.Send(command);
            return NoContent();
        }

        [HttpDelete("{id}")]
        [HasPermission(Permissions.Orgs.Update)]
        public async Task<IActionResult> Delete(Guid id)
        {
            await _sender.Send(new DeleteStopCommand(id));
            return NoContent();
        }
    }
}
