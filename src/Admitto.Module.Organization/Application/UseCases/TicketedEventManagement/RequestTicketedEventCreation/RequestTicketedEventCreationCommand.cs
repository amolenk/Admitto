using Amolenk.Admitto.Module.Shared.Application.Messaging;

namespace Amolenk.Admitto.Module.Organization.Application.UseCases.TicketedEventManagement.RequestTicketedEventCreation;

/// <summary>
/// Accepts a ticketed-event creation request on behalf of a team. On success, a
/// <c>TeamEventCreationRequest</c> is persisted in <c>Pending</c>, the team's
/// <c>PendingEventCount</c> is incremented, and a
/// <c>TicketedEventCreationRequested</c> integration event is outboxed. Returns
/// the newly assigned <c>CreationRequestId</c>.
/// </summary>
internal sealed record RequestTicketedEventCreationCommand(
    Guid TeamId,
    Guid RequesterId,
    string Slug,
    string Name,
    string WebsiteUrl,
    string BaseUrl,
    DateTimeOffset StartsAt,
    DateTimeOffset EndsAt,
    string TimeZone) : Command<Guid>;
