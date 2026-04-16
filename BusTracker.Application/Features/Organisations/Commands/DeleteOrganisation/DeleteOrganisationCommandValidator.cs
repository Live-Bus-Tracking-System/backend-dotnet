using FluentValidation;

namespace BusTracker.Application.Features.Organisations.Commands.DeleteOrganisation
{
    public class DeleteOrganisationCommandValidator : AbstractValidator<DeleteOrganisationCommand>
    {
        public DeleteOrganisationCommandValidator()
        {
            RuleFor(x => x.OrganisationId)
                .NotEmpty().WithMessage("Organisation ID is required.");
        }
    }
}
