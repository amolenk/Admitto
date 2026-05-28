using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Organization.Application.UseCases.TicketedEvents.RequestTicketedEventCreation;

/// <summary>
/// Accepts a ticketed-event creation request on behalf of a team. On success, a
/// <c>TeamEventCreationRequest</c> is persisted in <c>Pending</c>, the team's
/// <c>PendingEventCount</c> is incremented, and a
/// <c>TicketedEventCreationRequestedIntegrationEvent</c> integration event is outboxed. Returns
/// the newly assigned <c>CreationRequestId</c>.
/// </summary>
internal sealed record RequestTicketedEventCreationCommand(
    Guid TeamId,
    Guid RequesterId,
    string Name,
    string WebsiteUrl,
    string BaseUrl,
    DateTimeOffset StartsAt,
    DateTimeOffset EndsAt,
    string TimeZone) : Command<Guid>;
