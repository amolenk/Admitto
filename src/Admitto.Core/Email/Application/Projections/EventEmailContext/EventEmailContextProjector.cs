using Amolenk.Admitto.Core.Email.Application.Persistence;
using Amolenk.Admitto.Core.Registrations.Contracts;
using Amolenk.Admitto.Core.Registrations.Contracts.IntegrationEvents;
using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Email.Application.Projections.EventEmailContext;

/// <summary>
/// Maintains the Email-owned <see cref="EventEmailContextView"/> projection from
/// Registrations integration events, mirroring the role-based
/// <c>ActivityLogProjector</c> pattern in Registrations. A single class owns all
/// projection writes (through <see cref="IEmailReadStore"/>) so the upsert logic
/// is not duplicated across slices. Schedule-affecting event data is projected
/// for the stable hourly evaluator.
/// </summary>
/// <remarks>
/// Idempotent under at-least-once delivery: every handler upserts via
/// <c>GetOrCreate</c> and tolerates partial, out-of-order updates. The unit of
/// work is committed by the queue dispatcher after each handler, so projection
/// writes and the reconfirm-trigger command observe the same transaction-scoped
/// <c>DbContext</c>.
/// </remarks>
internal sealed class EventEmailContextProjector(
    IEmailReadStore readStore)
    : IIntegrationEventHandler<TicketedEventCreatedIntegrationEvent>,
      IIntegrationEventHandler<TicketedEventDetailsChangedIntegrationEvent>,
      IIntegrationEventHandler<TicketedEventReconfirmPolicyChangedIntegrationEvent>,
      IIntegrationEventHandler<TicketedEventSelfServiceTicketTypeCountChangedIntegrationEvent>,
      IIntegrationEventHandler<TicketedEventArchivedIntegrationEvent>
{
    public async ValueTask HandleAsync(
        TicketedEventCreatedIntegrationEvent integrationEvent,
        CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var view = await GetOrCreateAsync(
            TeamId.From(integrationEvent.TeamId),
            TicketedEventId.From(integrationEvent.TicketedEventId),
            now,
            cancellationToken);

        var applied = view.UpdateEventContext(
            integrationEvent.TicketedEventVersion,
            integrationEvent.Name,
            integrationEvent.WebsiteUrl,
            integrationEvent.PublicSlug,
            integrationEvent.TimeZone,
            integrationEvent.SelfServiceTicketTypeCount,
            integrationEvent.ReconfirmPolicy,
            integrationEvent.IsArchived,
            now);

    }

    public async ValueTask HandleAsync(
        TicketedEventDetailsChangedIntegrationEvent integrationEvent,
        CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var view = await GetOrCreateAsync(
            TeamId.From(integrationEvent.TeamId),
            TicketedEventId.From(integrationEvent.TicketedEventId),
            now,
            cancellationToken);

        var applied = view.UpdateDetails(
            integrationEvent.TicketedEventVersion,
            integrationEvent.Name,
            integrationEvent.WebsiteUrl,
            integrationEvent.PublicSlug,
            integrationEvent.TimeZone,
            now);

    }

    public async ValueTask HandleAsync(
        TicketedEventReconfirmPolicyChangedIntegrationEvent integrationEvent,
        CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var view = await GetOrCreateAsync(
            TeamId.From(integrationEvent.TeamId),
            TicketedEventId.From(integrationEvent.TicketedEventId),
            now,
            cancellationToken);

        var applied = view.UpdateReconfirmPolicy(
            integrationEvent.TicketedEventVersion,
            integrationEvent.Policy,
            now);
        if (!applied)
            return;

    }

    public async ValueTask HandleAsync(
        TicketedEventSelfServiceTicketTypeCountChangedIntegrationEvent integrationEvent,
        CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var view = await GetOrCreateAsync(
            TeamId.From(integrationEvent.TeamId),
            TicketedEventId.From(integrationEvent.TicketedEventId),
            now,
            cancellationToken);

        view.UpdateSelfServiceTicketTypeCount(
            integrationEvent.TicketCatalogVersion,
            integrationEvent.SelfServiceTicketTypeCount,
            now);
    }

    public async ValueTask HandleAsync(
        TicketedEventArchivedIntegrationEvent integrationEvent,
        CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var view = await GetOrCreateAsync(
            TeamId.From(integrationEvent.TeamId),
            TicketedEventId.From(integrationEvent.TicketedEventId),
            now,
            cancellationToken);

        var applied = view.MarkArchived(integrationEvent.TicketedEventVersion, now);
        if (!applied)
            return;

    }

    private async Task<EventEmailContextView> GetOrCreateAsync(
        TeamId teamId,
        TicketedEventId ticketedEventId,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        // Check entities already tracked in this unit of work first, so multiple
        // upserts for the same event within one transaction (e.g. out-of-order
        // events processed back-to-back before a commit) reuse the same row
        // instead of attempting to add a duplicate key.
        var tracked = readStore.EventEmailContexts.Local
            .FirstOrDefault(c => c.TeamId == teamId && c.TicketedEventId == ticketedEventId);
        if (tracked is not null)
            return tracked;

        var view = await readStore.EventEmailContexts
            .FirstOrDefaultAsync(
                c => c.TeamId == teamId && c.TicketedEventId == ticketedEventId,
                cancellationToken);

        if (view is not null)
            return view;

        view = EventEmailContextView.CreatePartial(teamId, ticketedEventId, now);
        readStore.EventEmailContexts.Add(view);
        return view;
    }

}
