using Amolenk.Admitto.Module.Organization.Application.Mapping;
using Amolenk.Admitto.Module.Organization.Application.Persistence;
using Amolenk.Admitto.Module.Organization.Domain.Entities;
using Amolenk.Admitto.Module.Shared.Application.Messaging;
using Amolenk.Admitto.Module.Shared.Kernel.ErrorHandling;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Module.Organization.Application.UseCases.TeamMembershipManagement.ChangeTeamMembershipRole;

internal sealed class ChangeTeamMembershipRoleHandler(IOrganizationWriteStore writeStore)
    : ICommandHandler<ChangeTeamMembershipRoleCommand>
{
    public async ValueTask HandleAsync(ChangeTeamMembershipRoleCommand command, CancellationToken cancellationToken)
    {
        var emailAddress = EmailAddress.From(command.EmailAddress);

        var user = await writeStore.Users
            .FirstOrDefaultAsync(u => u.EmailAddress == emailAddress, cancellationToken);

        if (user is null)
        {
            throw new BusinessRuleViolationException(Errors.UserNotFound(emailAddress));
        }

        user.ChangeTeamMembershipRole(TeamId.From(command.TeamId), command.NewRole.ToDomain());
    }

    internal static class Errors
    {
        public static Error UserNotFound(EmailAddress email) =>
            new(
                "user.not_found",
                "No user with the specified email address exists.",
                Type: ErrorType.NotFound,
                Details: new Dictionary<string, object?> { ["email"] = email.Value });
    }
}
