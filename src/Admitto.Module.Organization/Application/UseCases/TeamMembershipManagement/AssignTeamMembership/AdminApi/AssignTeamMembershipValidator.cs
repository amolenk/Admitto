using Amolenk.Admitto.Module.Shared.Application.Validation;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;
using FluentValidation;

namespace Amolenk.Admitto.Module.Organization.Application.UseCases.TeamMembershipManagement.AssignTeamMembership.AdminApi;

public sealed class AssignTeamMembershipValidator : AbstractValidator<AssignTeamMembershipHttpRequest>
{
    public AssignTeamMembershipValidator()
    {
        RuleFor(x => x.Email)
            .MustBeParseable(EmailAddress.TryFrom);

        RuleFor(x => x.Role)
            .NotNull()
            .IsInEnum();
    }
}