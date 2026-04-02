using Amolenk.Admitto.Module.Organization.Application.Persistence;
using Amolenk.Admitto.Module.Shared.Application.Messaging;
using Amolenk.Admitto.Module.Shared.Kernel.ErrorHandling;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Module.Organization.Application.UseCases.TeamMembershipManagement.RemoveTeamMembership;

internal sealed class RemoveTeamMembershipHandler(IOrganizationWriteStore writeStore)
    : ICommandHandler<RemoveTeamMembershipCommand>
{
    public async ValueTask HandleAsync(RemoveTeamMembershipCommand command, CancellationToken cancellationToken)
    {
        var emailAddress = EmailAddress.From(command.EmailAddress);

        var user = await writeStore.Users
            .FirstOrDefaultAsync(u => u.EmailAddress == emailAddress, cancellationToken);

        if (user is null)
        {
            throw new BusinessRuleViolationException(Errors.UserNotFound(emailAddress));
        }

        user.RemoveTeamMembership(TeamId.From(command.TeamId));
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
