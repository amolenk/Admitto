using System.Text.Json.Serialization;
using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Registrations.Contracts.IntegrationEvents;

/// <summary>
/// Published by the Registrations module after it successfully materialises a
/// <c>TicketedEvent</c> in response to a <c>TicketedEventCreationRequestedIntegrationEvent</c>.
/// The Organization module consumes this to mark the corresponding
/// <c>TeamEventCreationRequest</c> as <c>Created</c>, decrement
/// <c>PendingEventCount</c>, and increment <c>ActiveEventCount</c>. The
/// <paramref name="TimeZone"/> field carries the event's IANA zone so the
/// Email module's hourly evaluator can apply event-local quiet hours without
/// an additional read against Registrations.
/// </summary>
[method: JsonConstructor]
public sealed record TicketedEventCreatedIntegrationEvent(
    Guid CreationRequestId,
    Guid TeamId,
    Guid TicketedEventId,
    uint TicketedEventVersion,
    string Name,
    string WebsiteUrl,
    string PublicSlug,
    string TimeZone,
    int SelfServiceTicketTypeCount,
    TicketedEventReconfirmPolicySnapshot? ReconfirmPolicy,
    bool IsArchived) : IntegrationEvent;
