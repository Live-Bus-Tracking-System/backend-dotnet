using FluentValidation;

namespace BusTracker.Application.Features.Organisations.Commands.ConfirmOrgDeletion
{
    public class ConfirmOrgDeletionCommandValidator : AbstractValidator<ConfirmOrgDeletionCommand>
    {
        public ConfirmOrgDeletionCommandValidator()
        {
            RuleFor(x => x.OrganisationId)
                .NotEmpty()
                .WithMessage("Organisation ID is required.");

            RuleFor(x => x.ConfirmToken)
                .NotEmpty()
                .WithMessage("Confirmation token cannot be empty.");
        }
    }
}
