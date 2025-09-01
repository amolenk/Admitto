namespace Amolenk.Admitto.Application.UseCases.Attendees.RegisterAttendee;

public record RegisterAttendeeRequest(
    string Email,
    string FirstName,
    string LastName,
    List<AdditionalDetailDto> AdditionalDetails,
    List<TicketSelectionDto> Tickets,
    bool OnBehalfOf = false,
    string? VerificationToken = null);

public record AdditionalDetailDto(string Name, string Value);

public record TicketSelectionDto(string TicketTypeSlug, int Quantity);
