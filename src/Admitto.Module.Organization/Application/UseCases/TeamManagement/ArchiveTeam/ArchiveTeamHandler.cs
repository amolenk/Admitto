using Amolenk.Admitto.Module.Organization.Application.Persistence;
using Amolenk.Admitto.Module.Shared.Application.Messaging;
using Amolenk.Admitto.Module.Shared.Application.Persistence;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Module.Organization.Application.UseCases.TeamManagement.ArchiveTeam;

/// <summary>
/// Archives a team. The active/pending events guard is now enforced as an aggregate invariant
/// on <c>Team</c> itself (<c>ActiveEventCount == 0 &amp;&amp; PendingEventCount == 0</c>) — these
/// counters are advanced by the integration-event handlers reacting to Registrations events.
/// </summary>
/// <remarks>
/// Optimistic concurrency on <c>Team.Version</c> catches concurrent counter updates
/// (e.g. a creation request arriving between the read and the archive commit).
/// </remarks>
internal sealed class ArchiveTeamHandler(IOrganizationWriteStore writeStore)
    : ICommandHandler<ArchiveTeamCommand>
{
    public async ValueTask HandleAsync(ArchiveTeamCommand command, CancellationToken cancellationToken)
    {
        var team = await writeStore.Teams.GetAsync(
            TeamId.From(command.TeamId),
            command.ExpectedVersion,
            cancellationToken);

        team.Archive(DateTimeOffset.UtcNow);
    }
}
