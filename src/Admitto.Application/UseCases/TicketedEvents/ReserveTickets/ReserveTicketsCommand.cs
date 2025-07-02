namespace Amolenk.Admitto.Application.UseCases.TicketedEvents.ReserveTickets;

public record ReserveTicketsCommand(Guid TicketedEventId, Guid RegistrationId, IDictionary<Guid, int> Tickets)
    : ICommand
{
    public Guid Id { get; init; } = Guid.NewGuid();
}
