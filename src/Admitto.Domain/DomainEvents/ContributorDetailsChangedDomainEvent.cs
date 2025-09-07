using Amolenk.Admitto.Domain.ValueObjects;

namespace Amolenk.Admitto.Domain.DomainEvents;

public record ContributorDetailsChangedDomainEvent(
    Guid TicketedEventId,
    Guid ParticipantId,
    Guid ContributorId,
    string? Email,
    string? FirstName,
    string? LastName)
    : DomainEvent;
