namespace Amolenk.Admitto.Domain.DomainEvents;

public record CrewAssignmentAddedDomainEvent(
    Guid TeamId,
    Guid TicketedEventId,
    Guid AssignmentId,
    uint AssignmentVersion,
    string Email)
    : DomainEvent;
