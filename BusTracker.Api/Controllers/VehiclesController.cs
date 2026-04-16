using BusTracker.Api.Authorization;
using BusTracker.Application.Common.Auth;
using BusTracker.Application.Features.Vehicles.Commands.ActivateVehicle;
using BusTracker.Application.Features.Vehicles.Commands.DeactivateVehicle;
using BusTracker.Application.Features.Vehicles.Commands.DeleteVehicle;
using BusTracker.Application.Features.Vehicles.Commands.RegisterVehicle;
using BusTracker.Application.Features.Vehicles.Commands.UpdateVehicle;
using BusTracker.Application.Features.Vehicles.Queries.GetAllVehicles;
using BusTracker.Application.Features.Vehicles.Queries.GetVehicleById;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace BusTracker.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class VehiclesController : ControllerBase
    {
        private readonly ISender _sender;

        public VehiclesController(ISender sender)
        {
            _sender = sender;
        }

        /// <summary>Registers a new vehicle under the caller's organisation.</summary>
        [HttpPost]
        [HasPermission(Permissions.Vehicles.Create)]
        public async Task<IActionResult> Register([FromBody] RegisterVehicleCommand command)
        {
            var dto = await _sender.Send(command);

            // If the vehicle is pending verification, return 202 Accepted (not yet active)
            // If immediately active (non-PublicTransit), return 201 Created
            if (!dto.IsActive)
                return Accepted(dto);

            return CreatedAtAction(nameof(GetById), new { id = dto.VehicleId }, dto);
        }

        /// <summary>Returns all vehicles visible to the caller. SuperAdmins may filter by OrganisationId.</summary>
        [HttpGet]
        [HasPermission(Permissions.Vehicles.Read)]
        public async Task<IActionResult> GetAll([FromQuery] GetAllVehiclesQuery query)
        {
            return Ok(await _sender.Send(query));
        }

        /// <summary>Returns a single vehicle by ID.</summary>
        [HttpGet("{id}")]
        [HasPermission(Permissions.Vehicles.Read)]
        public async Task<IActionResult> GetById(Guid id)
        {
            return Ok(await _sender.Send(new GetVehicleByIdQuery(id)));
        }

        /// <summary>Updates mutable fields of a vehicle (license plate, tracker ID, name, capacity).</summary>
        [HttpPut("{id}")]
        [HasPermission(Permissions.Vehicles.Update)]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateVehicleCommand command)
        {
            if (id != command.VehicleId)
                return BadRequest("ID in route must match VehicleId in payload.");

            return Ok(await _sender.Send(command));
        }

        /// <summary>Marks a vehicle as active.</summary>
        [HttpPut("{id}/activate")]
        [HasPermission(Permissions.Vehicles.Deactivate)]
        public async Task<IActionResult> Activate(Guid id)
        {
            await _sender.Send(new ActivateVehicleCommand(id));
            return NoContent();
        }

        /// <summary>Marks a vehicle as inactive (prevents route assignments).</summary>
        [HttpPut("{id}/deactivate")]
        [HasPermission(Permissions.Vehicles.Deactivate)]
        public async Task<IActionResult> Deactivate(Guid id)
        {
            await _sender.Send(new DeactivateVehicleCommand(id));
            return NoContent();
        }

        /// <summary>Soft-deletes a vehicle permanently.</summary>
        [HttpDelete("{id}")]
        [HasPermission(Permissions.Vehicles.Delete)]
        public async Task<IActionResult> Delete(Guid id)
        {
            await _sender.Send(new DeleteVehicleCommand(id));
            return NoContent();
        }
    }
}
