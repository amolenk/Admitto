namespace Amolenk.Admitto.Application.UseCases.TicketedEvents.GetTicketTypes;

public record GetTicketTypesResponse(TicketTypeDto[] TicketTypes);

public record TicketTypeDto(string Name, string SlotName, int MaxCapacity);
