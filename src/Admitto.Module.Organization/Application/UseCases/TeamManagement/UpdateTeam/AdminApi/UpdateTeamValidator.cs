using Amolenk.Admitto.Module.Shared.Application.Validation;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;
using FluentValidation;

namespace Amolenk.Admitto.Module.Organization.Application.UseCases.TeamManagement.UpdateTeam.AdminApi;

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