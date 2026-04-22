using Amolenk.Admitto.Module.Shared.Application.Messaging;

namespace Amolenk.Admitto.Module.Registrations.Contracts.IntegrationEvents;

/// <summary>
/// Published by the Registrations module after it successfully materialises a
/// <c>TicketedEvent</c> in response to a <c>TicketedEventCreationRequested</c>.
/// The Organization module consumes this to mark the corresponding
/// <c>TeamEventCreationRequest</c> as <c>Created</c>, decrement
/// <c>PendingEventCount</c>, and increment <c>ActiveEventCount</c>.
/// </summary>
public sealed record TicketedEventCreated(
    Guid CreationRequestId,
    Guid TeamId,
    Guid TicketedEventId,
    string Slug) : IntegrationEvent;
