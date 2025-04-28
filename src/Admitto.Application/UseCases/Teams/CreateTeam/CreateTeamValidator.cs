namespace Amolenk.Admitto.Application.UseCases.Teams.CreateTeam;

public class CreateTeamValidator : AbstractValidator<CreateTeamRequest>
{
    public CreateTeamValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Event name is required.")
            .MaximumLength(100).WithMessage("Event name must be 100 characters or less.");
    }
}