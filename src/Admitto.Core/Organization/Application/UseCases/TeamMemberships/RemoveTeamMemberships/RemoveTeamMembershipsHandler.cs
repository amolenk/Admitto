using Amolenk.Admitto.Core.Organization.Application.Persistence;
using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Organization.Application.UseCases.TeamMemberships.RemoveTeamMemberships;

internal sealed class RemoveTeamMembershipsHandler(IOrganizationWriteStore writeStore)
    : ICommandHandler<RemoveTeamMembershipsCommand>
{
    public async ValueTask HandleAsync(RemoveTeamMembershipsCommand command, CancellationToken cancellationToken)
    {
        var teamId = TeamId.From(command.TeamId);

        var members = await writeStore.Users
            .Where(u => u.Memberships.Any(m => m.Id == teamId))
            .ToListAsync(cancellationToken);

        foreach (var member in members)
        {
            member.RemoveTeamMembership(teamId);
        }
    }
}
