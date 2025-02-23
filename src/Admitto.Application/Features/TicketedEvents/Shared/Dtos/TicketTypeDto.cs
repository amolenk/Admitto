namespace Amolenk.Admitto.Application.Features.TicketedEvents.Shared.Dtos;

public record TicketTypeDto(string Name, DateTime StartDateTime, DateTime EndDateTime, int MaxCapacity);
