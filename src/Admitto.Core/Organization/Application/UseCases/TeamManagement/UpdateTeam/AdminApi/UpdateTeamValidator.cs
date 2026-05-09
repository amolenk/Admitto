using Amolenk.Admitto.Core.Shared.Application.Validation;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;
using FluentValidation;

namespace Amolenk.Admitto.Core.Organization.Application.UseCases.TeamManagement.UpdateTeam.AdminApi;

public sealed class UpdateTeamValidator : AbstractValidator<UpdateTeamHttpRequest>
{
    public UpdateTeamValidator()
    {
        RuleFor(x => x.Name)
            .MustBeNullOrParseable(DisplayName.TryFrom);

        RuleFor(x => x.EmailAddress)
            .MustBeNullOrParseable(EmailAddress.TryFrom);
    }
}