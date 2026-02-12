using Amolenk.Admitto.Organization.Application.Persistence;
using Amolenk.Admitto.Organization.Domain.Entities;
using Amolenk.Admitto.Shared.Application.Messaging;

namespace Amolenk.Admitto.Organization.Application.UseCases.Users.AssignTeamMembership;

internal sealed class AssignTeamMembershipHandler(IOrganizationWriteStore writeStore)
    : ICommandHandler<AssignTeamMembershipCommand>
{
    public async ValueTask HandleAsync(AssignTeamMembershipCommand command, CancellationToken cancellationToken)
    {
        var user = await writeStore.Users
            .FirstOrDefaultAsync(u => u.EmailAddress == command.EmailAddress, cancellationToken);

        if (user is null)
        {
            user = User.Create(command.EmailAddress);
            writeStore.Users.Add(user);
        }

        user.AddTeamMembership(command.TeamId, command.Role);
    }
}