using Amolenk.Admitto.Domain.ValueObjects;

namespace Amolenk.Admitto.Application.UseCases.Attendees.ChangeTickets;

/// <summary>
/// Represents a command to change tickets for a registered attendee.
/// </summary>
public record ChangeTicketsCommand(
    Guid TicketedEventId,
    Guid AttendeeId,
    IList<TicketSelection> RequestedTickets,
    bool AdminOnBehalfOf = false)
    : Command;