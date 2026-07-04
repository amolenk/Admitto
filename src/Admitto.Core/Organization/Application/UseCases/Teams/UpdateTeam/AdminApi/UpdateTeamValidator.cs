using Amolenk.Admitto.Core.Organization.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Application.Validation;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;
using FluentValidation;

namespace Amolenk.Admitto.Core.Organization.Application.UseCases.Teams.UpdateTeam.AdminApi;

public sealed class UpdateTeamValidator : AbstractValidator<UpdateTeamHttpRequest>
{
    public UpdateTeamValidator()
    {
        RuleFor(x => x.Name)
            .MustBeNullOrParseable(TeamName.TryFrom);

        RuleFor(x => x.AccentColor)
            .MustBeNullOrParseable(TeamAccentColor.TryFrom);

        RuleFor(x => x.ReplyToEmailAddress)
            .MustBeNullOrParseable(EmailAddress.TryFrom);

        RuleFor(x => x.ReplyToEmailAddress)
            .Null()
            .When(x => x.ClearReplyToEmailAddress == true);
    }
}
