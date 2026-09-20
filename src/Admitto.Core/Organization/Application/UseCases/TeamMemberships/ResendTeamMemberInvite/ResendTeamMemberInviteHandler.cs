using Amolenk.Admitto.Core.Organization.Application.ExternalUsers;
using Amolenk.Admitto.Core.Organization.Application.Persistence;
using Amolenk.Admitto.Core.Organization.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Amolenk.Admitto.Core.Shared.Kernel.ErrorHandling;

namespace Amolenk.Admitto.Core.Organization.Application.UseCases.TeamMemberships.ResendTeamMemberInvite;

internal sealed class ResendTeamMemberInviteHandler(
    IOrganizationWriteStore writeStore,
    IExternalUserDirectory userDirectory)
    : ICommandHandler<ResendTeamMemberInviteCommand>
{
    public async ValueTask HandleAsync(ResendTeamMemberInviteCommand command, CancellationToken cancellationToken)
    {
        var emailAddress = EmailAddress.From(command.EmailAddress);
        var teamId = TeamId.From(command.TeamId);

        var user = await writeStore.Users
            .FirstOrDefaultAsync(u => u.EmailAddress == emailAddress, cancellationToken);

        if (user is null)
        {
            throw new BusinessRuleViolationException(TeamMembershipErrors.UserNotFound(emailAddress));
        }

        user.EnsureIsTeamMember(teamId);

        var externalUserId = await userDirectory.InviteUserAsync(user.EmailAddress.Value, cancellationToken);

        user.AssignExternalUserId(ExternalUserId.From(externalUserId));
    }
}
