using Amolenk.Admitto.Domain.ValueObjects;

namespace Amolenk.Admitto.Domain.DomainEvents;

/// <summary>
/// Represents a domain event that is triggered when a person registers for a ticketed event.
/// </summary>
public record AttendeeRegisteredDomainEvent(
    Guid TeamId,
    Guid TicketedEventId,
    Guid RegistrationId,
    string Email,
    string FirstName,
    string LastName,
    IList<AdditionalDetail> AdditionalDetails,
    IList<TicketSelection> Tickets)
    : DomainEvent;