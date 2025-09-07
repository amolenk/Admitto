using Amolenk.Admitto.Domain.ValueObjects;

namespace Amolenk.Admitto.Domain.DomainEvents;

public record ContributorRolesChangedDomainEvent(
    Guid TicketedEventId,
    Guid ParticipantId,
    Guid ContributorId,
    List<ContributorRole> PreviousRoles,
    List<ContributorRole> CurrentRoles)
    : DomainEvent;
