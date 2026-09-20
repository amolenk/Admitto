using Amolenk.Admitto.Core.Organization.Application.Persistence;
using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Amolenk.Admitto.Core.Shared.Kernel.ErrorHandling;

namespace Amolenk.Admitto.Core.Organization.Application.UseCases.TeamMemberships.RemoveTeamMembership;

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
            throw new BusinessRuleViolationException(TeamMembershipErrors.UserNotFound(emailAddress));
        }

        user.RemoveTeamMembership(TeamId.From(command.TeamId));
    }
}
