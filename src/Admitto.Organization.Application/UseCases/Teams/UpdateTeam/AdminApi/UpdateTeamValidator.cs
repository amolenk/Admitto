using Amolenk.Admitto.Shared.Application.Validation;
using Amolenk.Admitto.Shared.Kernel.ValueObjects;
using FluentValidation;

namespace Amolenk.Admitto.Organization.Application.UseCases.Teams.UpdateTeam.AdminApi;

public sealed class UpdateTeamValidator : AbstractValidator<UpdateTeamHttpRequest>
{
    public UpdateTeamValidator()
    {
        RuleFor(x => x.Slug)
            .MustBeNullOrParseable(Slug.TryFrom);

        RuleFor(x => x.Name)
            .MustBeNullOrParseable(DisplayName.TryFrom);

        RuleFor(x => x.EmailAddress)
            .MustBeNullOrParseable(EmailAddress.TryFrom);
    }
}