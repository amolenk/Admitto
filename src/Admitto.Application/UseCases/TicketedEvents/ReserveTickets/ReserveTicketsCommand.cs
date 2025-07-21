using Amolenk.Admitto.Domain.ValueObjects;

namespace Amolenk.Admitto.Application.UseCases.TicketedEvents.ReserveTickets;

public record ReserveTicketsCommand(
    Guid TicketedEventId,
    Guid AttendeeId,
    List<TicketSelection> Tickets,
    bool IgnoreAvailability = false)
    : Command;
