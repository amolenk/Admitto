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
            integrationEvent.ReplyToEmailAddress,
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
            integrationEvent.ReplyToEmailAddress,
            integrationEvent.TeamVersion,
            cancellationToken);
    }

    private async ValueTask ProjectAsync(
        Guid teamIdValue,
        string name,
        string accentColor,
        string? replyToEmailAddress,
        uint teamVersion,
        CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var teamId = TeamId.From(teamIdValue);
        var view = await GetOrCreateAsync(teamId, now, cancellationToken);

        view.UpdateTeamContext(name, accentColor, replyToEmailAddress, teamVersion, now);
    }

    private async Task<TeamEmailContextView> GetOrCreateAsync(
        TeamId teamId,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var tracked = readStore.TeamEmailContexts.Local
            .FirstOrDefault(c => c.TeamId == teamId);
        if (tracked is not null)
            return tracked;

        var view = await readStore.TeamEmailContexts
            .FirstOrDefaultAsync(c => c.TeamId == teamId, cancellationToken);

        if (view is not null)
            return view;

        view = TeamEmailContextView.CreatePartial(teamId, now);
        readStore.TeamEmailContexts.Add(view);
        return view;
    }
}
