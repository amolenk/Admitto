namespace Amolenk.Admitto.Application.UseCases.AttendeeRegistrations.RegisterAttendee;

public record RegisterAttendeeRequest(
    string Email,
    string FirstName,
    string LastName,
    List<AdditionalDetailDto> AdditionalDetails,
    List<TicketSelectionDto> Tickets,
    string VerificationToken);

public record AdditionalDetailDto(string Name, string Value);

public record TicketSelectionDto(string TicketTypeSlug, int Quantity);
