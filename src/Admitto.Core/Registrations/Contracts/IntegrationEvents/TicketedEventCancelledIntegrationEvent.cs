using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Registrations.Contracts.IntegrationEvents;

/// <summary>
/// Published by the Registrations module when a <c>TicketedEvent</c> transitions
/// to <c>Cancelled</c>. The Organization module consumes this to advance the
/// owning team's counters (active → cancelled).
/// </summary>
public sealed record TicketedEventCancelledIntegrationEvent(
    Guid TeamId,
    Guid TicketedEventId) : IntegrationEvent;
