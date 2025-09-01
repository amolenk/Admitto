using Amolenk.Admitto.Domain.ValueObjects;

namespace Amolenk.Admitto.Domain.DomainEvents;

/// <summary>
/// Represents a domain event that is triggered when tickets are claimed.
/// </summary>
public record TicketsClaimedDomainEvent(
    Guid TicketedEventId,
    Guid ParticipantId,
    string Email,
    string FirstName,
    string LastName,
    IList<AdditionalDetail> AdditionalDetails,
    IList<TicketSelection> ClaimedTickets)
    : DomainEvent;