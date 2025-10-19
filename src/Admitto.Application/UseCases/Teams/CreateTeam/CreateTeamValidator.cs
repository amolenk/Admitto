using Amolenk.Admitto.Application.Common.Validation;

namespace Amolenk.Admitto.Application.UseCases.Teams.CreateTeam;

public class CreateTeamValidator : AbstractValidator<CreateTeamRequest>
{
    public CreateTeamValidator()
    {
        RuleFor(x => x.Slug)
            .NotNull()
            .MinimumLength(2)
            .MaximumLength(50)
            .Slug().WithMessage("Team slug must contain only lowercase letters, numbers, and hyphens (no leading, trailing, or consecutive hyphens).");

        RuleFor(x => x.Name)
            .NotNull()
            .MinimumLength(2)
            .MaximumLength(50);
        
        RuleFor(x => x.Email)
            .NotNull()
            .EmailAddress()
            .MinimumLength(2)
            .MaximumLength(254);
        
        RuleFor(x => x.EmailServiceConnectionString)
            .NotNull()
            .MinimumLength(2)
            .MaximumLength(100);
    }
}