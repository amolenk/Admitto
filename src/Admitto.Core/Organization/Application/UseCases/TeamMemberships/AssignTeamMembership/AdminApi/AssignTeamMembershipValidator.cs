using Amolenk.Admitto.Core.Shared.Application.Validation;
using FluentValidation;

namespace Amolenk.Admitto.Core.Organization.Application.UseCases.TeamMemberships.AssignTeamMembership.AdminApi;

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