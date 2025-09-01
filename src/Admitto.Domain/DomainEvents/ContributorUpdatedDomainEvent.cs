using Amolenk.Admitto.Domain.ValueObjects;

namespace Amolenk.Admitto.Domain.DomainEvents;

public record ContributorUpdatedDomainEvent(
    Guid TicketedEventId,
    Guid ParticipantId,
    Guid ContributorId,
    string? Email,
    string? FirstName,
    string? LastName,
    List<ContributorRole>? Roles)
    : DomainEvent;
