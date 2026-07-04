using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Registrations.Contracts.IntegrationEvents;

/// <summary>
/// Published by the Registrations module when it rejects a
/// <c>TicketedEventCreationRequestedIntegrationEvent</c> (e.g. because of a duplicate
/// <c>(TeamId, Slug)</c>). The Organization module consumes this to mark the
/// corresponding <c>TeamEventCreationRequest</c> as <c>Rejected</c> and
/// decrement <c>PendingEventCount</c>.
/// </summary>
/// <param name="Reason">
/// Stable machine-readable reason code (e.g. <c>"duplicate_slug"</c>).
/// Exposed to admins via the creation-status endpoint.
/// </param>
public sealed record TicketedEventCreationRejectedIntegrationEvent(
    Guid CreationRequestId,
    Guid TeamId,
    string Reason) : IntegrationEvent;
