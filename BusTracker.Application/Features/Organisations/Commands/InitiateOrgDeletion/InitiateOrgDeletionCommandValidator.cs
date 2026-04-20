using FluentValidation;

namespace BusTracker.Application.Features.Organisations.Commands.InitiateOrgDeletion
{
    public class InitiateOrgDeletionCommandValidator : AbstractValidator<InitiateOrgDeletionCommand>
    {
        public InitiateOrgDeletionCommandValidator()
        {
            RuleFor(x => x.OrganisationId)
                .NotEmpty()
                .WithMessage("Organisation ID is required.");

            RuleFor(x => x.Password)
                .NotEmpty()
                .WithMessage("Password cannot be empty.");
        }
    }
}
