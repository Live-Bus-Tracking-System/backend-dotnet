using FluentValidation;

namespace BusTracker.Application.Features.Organisations.Commands.SuspendOrganisation
{
    public class SuspendOrganisationCommandValidator : AbstractValidator<SuspendOrganisationCommand>
    {
        public SuspendOrganisationCommandValidator()
        {
            RuleFor(x => x.OrganisationId)
                .NotEmpty().WithMessage("Organisation ID is required to perform a suspension.");

            RuleFor(x => x.Reason)
                .MaximumLength(1000).WithMessage("Suspension reason must not exceed 1000 characters.");
        }
    }
}
