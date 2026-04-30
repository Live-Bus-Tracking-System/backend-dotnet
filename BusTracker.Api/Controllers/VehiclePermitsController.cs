using BusTracker.Api.Authorization;
using BusTracker.Application.Common.Auth;
using BusTracker.Application.Features.Permits.Commands.ReviewPermit;
using BusTracker.Application.Features.Permits.Commands.VerifyDocument;
using BusTracker.Application.Features.Permits.Queries.GetPendingPermitDetail;
using BusTracker.Application.Features.Permits.Queries.GetPendingVehiclePermits;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace BusTracker.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class VehiclePermitsController : ControllerBase
    {
        private readonly ISender _sender;

        public VehiclePermitsController(ISender sender)
        {
            _sender = sender;
        }

        /// <summary>Gets a paginated list of all pending vehicle permits.</summary>
        [HttpGet("pending")]
        [HasPermission(Permissions.Permits.Read)]
        public async Task<IActionResult> GetPending([FromQuery] GetPendingVehiclePermitsQuery query)
        {
            return Ok(await _sender.Send(query));
        }

        /// <summary>Gets full details of a pending permit including vehicle info and document IDs.</summary>
        [HttpGet("pending/{id}")]
        [HasPermission(Permissions.Permits.Read)]
        public async Task<IActionResult> GetPendingDetail(Guid id)
        {
            return Ok(await _sender.Send(new GetPendingPermitDetailQuery(id)));
        }

        /// <summary>Verifies an individual compliance document attached to the permit.</summary>
        [HttpPost("{permitId}/documents/{documentId}/verify")]
        [HasPermission(Permissions.Permits.Approve)]
        public async Task<IActionResult> VerifyDocument(Guid permitId, Guid documentId, [FromBody] VerifyComplianceDocumentCommand command)
        {
            if (permitId != command.PermitId || documentId != command.DocumentId)
                return BadRequest("Path IDs must match payload IDs.");

            await _sender.Send(command);
            return NoContent();
        }

        /// <summary>Issues final approval (assigning a route) or rejection for the permit.</summary>
        [HttpPost("{permitId}/review")]
        [HasPermission(Permissions.Permits.Approve)]
        public async Task<IActionResult> ReviewPermit(Guid permitId, [FromBody] ReviewVehiclePermitCommand command)
        {
            if (permitId != command.PermitId)
                return BadRequest("Path ID must match payload ID.");

            await _sender.Send(command);
            return NoContent();
        }
    }
}
