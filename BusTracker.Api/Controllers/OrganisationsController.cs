using BusTracker.Api.Authorization;
using BusTracker.Application.Common.Auth;
using BusTracker.Application.Features.Organisations.Commands.ActivateOrganisation;
using BusTracker.Application.Features.Organisations.Commands.CreateOrganisation;
using BusTracker.Application.Features.Organisations.Commands.DeleteOrganisation;
using BusTracker.Application.Features.Organisations.Commands.InitiateOrgDeletion;
using BusTracker.Application.Features.Organisations.Commands.VerifyOrgDeletionOtp;
using BusTracker.Application.Features.Organisations.Commands.ConfirmOrgDeletion;
using BusTracker.Application.Features.Organisations.Commands.SuspendOrganisation;
using BusTracker.Application.Features.Organisations.Commands.UpdateOrganisation;
using BusTracker.Application.Features.Organisations.Queries.GetAllOrganisations;
using BusTracker.Application.Features.Organisations.Queries.GetOrganisationById;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BusTracker.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OrganisationsController : ControllerBase
    {
        private readonly ISender _sender;

        public OrganisationsController(ISender sender)
        {
            _sender = sender;
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Create([FromBody] CreateOrganisationCommand command)
        {
            var orgId = await _sender.Send(command);
            return CreatedAtAction(nameof(GetById), new { id = orgId }, new { Id = orgId });
        }

        [HttpGet]
        [HasPermission(Permissions.Orgs.ReadAll)]
        public async Task<IActionResult> GetAll([FromQuery] GetAllOrganisationsQuery query)
        {
            return Ok(await _sender.Send(query));
        }

        [HttpGet("{id}")]
        [HasPermission(Permissions.Orgs.Read)]
        public async Task<IActionResult> GetById(Guid id)
        {
            return Ok(await _sender.Send(new GetOrganisationByIdQuery(id)));
        }

        [HttpPut]
        [HasPermission(Permissions.Orgs.Update)]
        public async Task<IActionResult> Update([FromBody] UpdateOrganisationCommand command)
        {
            return Ok(await _sender.Send(command));
        }

        [HttpPut("{id}/activate")]
        [HasPermission(Permissions.Orgs.Activate)]
        public async Task<IActionResult> Activate(Guid id)
        {
            await _sender.Send(new ActivateOrganisationCommand(id));
            return NoContent();
        }

        [HttpPut("{id}/suspend")]
        [HasPermission(Permissions.Orgs.Suspend)]
        public async Task<IActionResult> Suspend(Guid id, [FromBody] SuspendOrganisationCommand command)
        {
            if (id != command.OrganisationId)
            {
                return BadRequest("ID in route must match ID in payload.");
            }

            await _sender.Send(command);
            return NoContent();
        }

        [HttpDelete("{id}")]
        [HasPermission(Permissions.Orgs.Delete)]
        public async Task<IActionResult> Delete(Guid id)
        {
            await _sender.Send(new DeleteOrganisationCommand(id));
            return NoContent();
        }

        [HttpPost("{id}/deletion/initiate")]
        [HasPermission(Permissions.Orgs.Delete)]
        public async Task<IActionResult> InitiateDeletion(Guid id, [FromBody] InitiateOrgDeletionCommand command)
        {
            var result = await _sender.Send(command with { OrganisationId = id });
            return Ok(result);
        }

        [HttpPost("{id}/deletion/verify-otp")]
        [HasPermission(Permissions.Orgs.Delete)]
        public async Task<IActionResult> VerifyDeletionOtp(Guid id, [FromBody] VerifyOrgDeletionOtpCommand command)
        {
            var result = await _sender.Send(command with { OrganisationId = id });
            return Ok(result);
        }

        [HttpPost("{id}/deletion/confirm")]
        [HasPermission(Permissions.Orgs.Delete)]
        public async Task<IActionResult> ConfirmDeletion(Guid id, [FromBody] ConfirmOrgDeletionCommand command)
        {
            await _sender.Send(command with { OrganisationId = id });

            Response.Cookies.Delete("access_token");
            Response.Cookies.Delete("refresh_token");

            return NoContent();
        }
    }
}
