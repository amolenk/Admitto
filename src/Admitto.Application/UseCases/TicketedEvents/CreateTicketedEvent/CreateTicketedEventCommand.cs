using Amolenk.Admitto.Application.Common.DTOs;

namespace Amolenk.Admitto.Application.UseCases.TicketedEvents.CreateTicketedEvent;

public record CreateTicketedEventCommand(
    string Name,
    DateOnly StartDay,
    DateOnly EndDay,
    DateTime SalesStartDateTime,
    DateTime SalesEndDateTime,
    IEnumerable<TicketTypeDto>? TicketTypes) : ICommand
{
    public Guid CommandId { get; } = Guid.NewGuid();
}

public record CreateTicketedEventResult(Guid Id);
