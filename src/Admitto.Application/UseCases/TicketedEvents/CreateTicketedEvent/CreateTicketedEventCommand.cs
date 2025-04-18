namespace Amolenk.Admitto.Application.UseCases.TicketedEvents.CreateTicketedEvent;

public record CreateTicketedEventCommand(
    Guid TeamId,
    string Name,
    DateOnly StartDay,
    DateOnly EndDay,
    DateTime SalesStartDateTime,
    DateTime SalesEndDateTime,
    IEnumerable<TicketTypeDto>? TicketTypes) : ICommand
{
    public Guid Id { get; } = Guid.NewGuid();
}

public record TicketTypeDto(string Name, DateTime StartDateTime, DateTime EndDateTime, int MaxCapacity);