using Amolenk.Admitto.Core.Organization.Application.Mapping;
using Amolenk.Admitto.Core.Organization.Application.Persistence;
using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Amolenk.Admitto.Core.Shared.Kernel.ErrorHandling;

namespace Amolenk.Admitto.Core.Organization.Application.UseCases.TeamMemberships.ChangeTeamMembershipRole;

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
            throw new BusinessRuleViolationException(TeamMembershipErrors.UserNotFound(emailAddress));
        }

        user.ChangeTeamMembershipRole(TeamId.From(command.TeamId), command.NewRole.ToDomain());
    }
}
