using Amolenk.Admitto.Shared.Application.Validation;
using Amolenk.Admitto.Shared.Kernel.ValueObjects;
using FluentValidation;

namespace Amolenk.Admitto.Organization.Application.UseCases.Users.AssignTeamMembership.AdminApi;

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