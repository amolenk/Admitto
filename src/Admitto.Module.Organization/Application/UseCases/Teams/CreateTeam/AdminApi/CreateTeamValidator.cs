using Amolenk.Admitto.Module.Shared.Application.Validation;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;
using FluentValidation;

namespace Amolenk.Admitto.Module.Organization.Application.UseCases.Teams.CreateTeam.AdminApi;

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