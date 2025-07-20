using Amolenk.Admitto.Domain.ValueObjects;

namespace Amolenk.Admitto.Application.UseCases.TicketedEvents.ReserveTickets;

public record ReserveTicketsCommand(
    Guid TicketedEventId,
    Guid RegistrationId,
    RegistrationType RegistrationType,
    IDictionary<string, int> Tickets)
    : Command;
