namespace Amolenk.Admitto.Application.UseCases.Attendees.UpdateAttendee;

public record UpdateAttendeeRequest(List<TicketSelectionDto> Tickets);

public record TicketSelectionDto(string TicketTypeSlug, int Quantity);
