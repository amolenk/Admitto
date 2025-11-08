namespace Amolenk.Admitto.Application.UseCases.Attendees.RemoveTickets;

/// <summary>
/// Represents a command to remove a specific ticket of a registered attendee.
/// </summary>
public record RemoveTicketsCommand(
    Guid TicketedEventId,
    Guid AttendeeId,
    string TicketTypeSlug)
    : Command;