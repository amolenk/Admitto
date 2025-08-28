using Amolenk.Admitto.Domain.ValueObjects;

namespace Amolenk.Admitto.Domain.DomainEvents;

public record ContributorRegisteredDomainEvent(
    Guid TeamId,
    Guid TicketedEventId,
    Guid RegistrationId,
    string Email,
    ContributorRole Role)
    : DomainEvent;
