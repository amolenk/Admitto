using Amolenk.Admitto.Core.Shared.Application.Validation;
using FluentValidation;

namespace Amolenk.Admitto.Core.Organization.Application.UseCases.Teams.UpdateTeam.AdminApi;

public sealed class UpdateTeamValidator : AbstractValidator<UpdateTeamHttpRequest>
{
    public UpdateTeamValidator()
    {
        RuleFor(x => x.Name)
            .MustBeNullOrParseable(TeamName.TryFrom);
    }
}