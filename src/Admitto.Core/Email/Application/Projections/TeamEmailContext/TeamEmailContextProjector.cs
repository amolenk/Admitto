using Amolenk.Admitto.Core.Email.Application.Persistence;
using Amolenk.Admitto.Core.Organization.Contracts.IntegrationEvents;
using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Email.Application.Projections.TeamEmailContext;

/// <summary>
/// Maintains the Email-owned <see cref="TeamEmailContextView"/> projection from
/// Organization integration events. Team context is intentionally projected
/// independently from event context so Organization only publishes team-level
/// facts and never enumerates events for Email.
/// </summary>
internal sealed class TeamEmailContextProjector(IEmailReadStore readStore)
    : IIntegrationEventHandler<TeamCreatedIntegrationEvent>,
      IIntegrationEventHandler<TeamDetailsUpdatedIntegrationEvent>
{
    public async ValueTask HandleAsync(
        TeamCreatedIntegrationEvent integrationEvent,
        CancellationToken cancellationToken)
    {
        await ProjectAsync(
            integrationEvent.TeamId,
            integrationEvent.Name,
            integrationEvent.AccentColor,
            integrationEvent.TeamVersion,
            cancellationToken);
    }

    public async ValueTask HandleAsync(
        TeamDetailsUpdatedIntegrationEvent integrationEvent,
        CancellationToken cancellationToken)
    {
        await ProjectAsync(
            integrationEvent.TeamId,
            integrationEvent.Name,
            integrationEvent.AccentColor,
            integrationEvent.TeamVersion,
            cancellationToken);
    }

    private async ValueTask ProjectAsync(
        Guid teamIdValue,
        string name,
        string accentColor,
        uint teamVersion,
        CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var teamId = TeamId.From(teamIdValue);
        var existing = await FindAsync(teamId, cancellationToken);

        if (existing is not null)
        {
            existing.UpdateTeamContext(name, accentColor, teamVersion, now);
            return;
        }

        readStore.TeamEmailContexts.Add(TeamEmailContextView.Create(
            teamId,
            name,
            accentColor,
            teamVersion,
            now));
    }

    private async Task<TeamEmailContextView?> FindAsync(TeamId teamId, CancellationToken cancellationToken)
    {
        var tracked = readStore.TeamEmailContexts.Local
            .FirstOrDefault(c => c.TeamId == teamId);
        if (tracked is not null)
            return tracked;

        return await readStore.TeamEmailContexts
            .FirstOrDefaultAsync(c => c.TeamId == teamId, cancellationToken);
    }
}
