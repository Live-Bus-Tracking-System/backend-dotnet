using FluentValidation;

namespace BusTracker.Application.Features.Organisations.Commands.ActivateOrganisation
{
    public class ActivateOrganisationCommandValidator : AbstractValidator<ActivateOrganisationCommand>
    {
        public ActivateOrganisationCommandValidator()
        {
            RuleFor(x => x.OrganisationId).NotEmpty().WithMessage("Organisation ID is required.");
        }
    }
}
