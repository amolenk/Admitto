using Amolenk.Admitto.Shared.Application.Validation;
using Amolenk.Admitto.Shared.Kernel.ValueObjects;
using FluentValidation;

namespace Amolenk.Admitto.Organization.Application.UseCases.Teams.CreateTeam.AdminApi;

public sealed class CreateTeamValidator : AbstractValidator<CreateTeamHttpRequest>
{
    public CreateTeamValidator()
    {
        RuleFor(x => x.Slug)
            .MustBeParseable(Slug.TryFrom);

        RuleFor(x => x.Name)
            .MustBeParseable(DisplayName.TryFrom);

        RuleFor(x => x.EmailAddress)
            .MustBeParseable(EmailAddress.TryFrom);
    }
}