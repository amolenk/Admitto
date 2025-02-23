using Amolenk.Admitto.Application.Features.TicketedEvents.Shared.Dtos;
using Amolenk.Admitto.Domain.Entities;

namespace Amolenk.Admitto.Application.Features.TicketedEvents.CreateTicketedEvent;

public record CreateTicketedEventCommand(
    string Name,
    DateOnly StartDay,
    DateOnly EndDay,
    DateTime SalesStartDateTime,
    DateTime SalesEndDateTime,
    IEnumerable<TicketTypeDto>? TicketTypes); //: IRequest<CreateTicketedEventResult>;

public record CreateTicketedEventResult(Guid Id);
