using Amolenk.Admitto.Module.Shared.Application.Validation;
using FluentValidation;

namespace Amolenk.Admitto.Module.Organization.Application.UseCases.TeamMembershipManagement.ChangeTeamMembershipRole.AdminApi;

public sealed class ChangeTeamMembershipRoleValidator : AbstractValidator<ChangeTeamMembershipRoleHttpRequest>
{
    public ChangeTeamMembershipRoleValidator()
    {
        RuleFor(x => x.NewRole)
            .NotNull()
            .IsInEnum();
    }
}
