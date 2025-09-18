using Amolenk.Admitto.Domain.ValueObjects;

namespace Amolenk.Admitto.Application.UseCases.Attendees.RegisterAttendee;

/// <summary>
/// Represents a command to register an attendee for a ticketed event.
/// </summary>
public record RegisterAttendeeCommand(
    Guid TeamId,
    Guid TicketedEventId,
    string Email,
    string FirstName,
    string LastName,
    IList<AdditionalDetail> AdditionalDetails,
    IList<TicketSelection> RequestedTickets,
    bool AdminOnBehalfOf = false)
    : Command;