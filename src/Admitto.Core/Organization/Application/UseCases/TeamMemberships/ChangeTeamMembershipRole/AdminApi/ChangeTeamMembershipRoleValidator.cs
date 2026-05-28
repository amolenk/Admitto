using Amolenk.Admitto.Core.Shared.Application.Validation;
using FluentValidation;

namespace Amolenk.Admitto.Core.Organization.Application.UseCases.TeamMemberships.ChangeTeamMembershipRole.AdminApi;

public sealed class ChangeTeamMembershipRoleValidator : AbstractValidator<ChangeTeamMembershipRoleHttpRequest>
{
    public ChangeTeamMembershipRoleValidator()
    {
        RuleFor(x => x.NewRole)
            .NotNull()
            .IsInEnum();
    }
}
