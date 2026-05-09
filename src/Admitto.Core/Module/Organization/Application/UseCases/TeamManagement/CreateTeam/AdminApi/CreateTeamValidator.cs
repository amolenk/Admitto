using Amolenk.Admitto.Core.Shared.Application.Validation;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;
using FluentValidation;

namespace Amolenk.Admitto.Core.Module.Organization.Application.UseCases.TeamManagement.CreateTeam.AdminApi;

public sealed class CreateTeamValidator : AbstractValidator<CreateTeamHttpRequest>
{
    public CreateTeamValidator()
    {
        RuleFor(x => x.Name)
            .MustBeParseable(DisplayName.TryFrom);

        RuleFor(x => x.EmailAddress)
            .MustBeParseable(EmailAddress.TryFrom);
    }
}