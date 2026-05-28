namespace Amolenk.Admitto.Core.Organization.Application.UseCases.TicketedEvents.GetEventCreationRequest;

/// <summary>
/// Projection of a <c>TeamEventCreationRequest</c> surfaced to the admin API.
/// </summary>
public sealed record EventCreationRequestDto(
    Guid CreationRequestId,
    Guid TeamId,
    Guid RequesterId,
    DateTimeOffset RequestedAt,
    string Status,
    DateTimeOffset? CompletedAt,
    Guid? TicketedEventId,
    string? RejectionReason);
