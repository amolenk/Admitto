using Amolenk.Admitto.Domain.ValueObjects;

namespace Amolenk.Admitto.Application.UseCases.TicketedEvents.RemoveAttendee;

/// <summary>
/// Represents a command to remove an attendee from a ticketed event.
/// </summary>
public record RemoveAttendeeCommand(Guid TicketedEventId, string Email, IList<TicketSelection> Tickets) : Command;