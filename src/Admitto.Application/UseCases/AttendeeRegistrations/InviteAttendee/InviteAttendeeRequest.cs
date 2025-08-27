namespace Amolenk.Admitto.Application.UseCases.AttendeeRegistrations.InviteAttendee;

public record InviteAttendeeRequest(
    string Email,
    string FirstName,
    string LastName,
    List<AdditionalDetailDto> AdditionalDetails,
    List<TicketSelectionDto> Tickets);

public record AdditionalDetailDto(string Name, string Value);

public record TicketSelectionDto(string TicketTypeSlug, int Quantity);
