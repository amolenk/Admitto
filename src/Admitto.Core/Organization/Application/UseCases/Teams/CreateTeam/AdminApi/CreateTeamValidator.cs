using Amolenk.Admitto.Core.Organization.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Application.Validation;
using FluentValidation;

namespace Amolenk.Admitto.Core.Organization.Application.UseCases.Teams.CreateTeam.AdminApi;

public sealed class CreateTeamValidator : AbstractValidator<CreateTeamHttpRequest>
{
    public CreateTeamValidator()
    {
        RuleFor(x => x.Name)
            .MustBeParseable(TeamName.TryFrom);

        RuleFor(x => x.AccentColor)
            .MustBeNullOrParseable(AccentColor.TryFrom);
    }
}
