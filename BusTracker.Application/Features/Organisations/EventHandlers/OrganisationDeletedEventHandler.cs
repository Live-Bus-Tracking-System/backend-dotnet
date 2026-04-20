using BusTracker.Application.Common.Events;
using BusTracker.Application.Common.Interfaces;
using BusTracker.Application.Common.Interfaces.Services;
using BusTracker.Domain.Events.Organisations;
using MediatR;
using Microsoft.Extensions.Logging;

namespace BusTracker.Application.Features.Organisations.EventHandlers
{
    /// <summary>
    /// Triggered after an Organisation is soft-deleted by a SuperAdmin.
    /// Extend this handler to trigger cleanup tasks (e.g. cascade deactivation of staff accounts).
    /// </summary>
    public class OrganisationDeletedEventHandler : INotificationHandler<DomainEventNotification<OrganisationDeletedDomainEvent>>
    {
        private readonly ILogger<OrganisationDeletedEventHandler> _logger;
        private readonly IIdentityService _identityService;
        private readonly IEmailService _emailService;
        private readonly ITemplateService _templateService;

        public OrganisationDeletedEventHandler(
            ILogger<OrganisationDeletedEventHandler> logger,
            IIdentityService identityService,
            IEmailService emailService,
            ITemplateService templateService)
        {
            _logger = logger;
            _identityService = identityService;
            _emailService = emailService;
            _templateService = templateService;
        }

        public async Task Handle(DomainEventNotification<OrganisationDeletedDomainEvent> notification, CancellationToken cancellationToken)
        {
            var e = notification.DomainEvent;
            _logger.LogWarning(
                "[Org Deleted] OrganisationId={OrganisationId} | Name={Name} | Email={Email}. Initiating cascading removal and session bust...",
                e.OrganisationId, e.Name, e.Email);

            // 1. Revert user roles, clear OrganisationId, and bump SecurityStamps.
            await _identityService.RemoveUsersFromOrganisationAsync(e.OrganisationId, cancellationToken);
            _logger.LogInformation("Successfully completed downstream application user resets for Org {Id}", e.OrganisationId);

            // 2. Dispatch final email notification.
            var emailModel = new
            {
                OrgName = e.Name
            };

            var htmlBody = await _templateService.RenderTemplateAsync("OrgDeletedEmail.html", emailModel);
            
            await _emailService.SendEmailAsync(
                to: e.Email,
                subject: "BusTracker - Organisation Permanently Deleted",
                htmlBody: htmlBody,
                cancellationToken: cancellationToken);
            
            _logger.LogInformation("Successfully dispatched final deletion notice to {Email}", e.Email);
        }
    }
}
