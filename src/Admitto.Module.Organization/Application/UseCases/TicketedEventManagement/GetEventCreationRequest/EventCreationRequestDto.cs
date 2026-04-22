namespace Amolenk.Admitto.Module.Organization.Application.UseCases.TicketedEventManagement.GetEventCreationRequest;

/// <summary>
/// Projection of a <c>TeamEventCreationRequest</c> surfaced to the admin API.
/// </summary>
public sealed record EventCreationRequestDto(
    Guid CreationRequestId,
    Guid TeamId,
    string RequestedSlug,
    Guid RequesterId,
    DateTimeOffset RequestedAt,
    string Status,
    DateTimeOffset? CompletedAt,
    Guid? TicketedEventId,
    string? RejectionReason);
