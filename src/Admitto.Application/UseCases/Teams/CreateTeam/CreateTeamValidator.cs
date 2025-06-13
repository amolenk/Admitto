namespace Amolenk.Admitto.Application.UseCases.Teams.CreateTeam;

public class CreateTeamValidator : AbstractValidator<CreateTeamRequest>
{
    public CreateTeamValidator()
    {
        RuleFor(x => x.Name)
            .MinimumLength(2).WithMessage("Team name must be at least 2 characters long.")
            .MaximumLength(100).WithMessage("Team name must be 100 characters or less.")
            .OverridePropertyName("name");
    }
}