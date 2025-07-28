namespace Amolenk.Admitto.Application.UseCases.Registrations.Register;

public record RegisterRequest(
    string Email,
    string FirstName,
    string LastName,
    List<AdditionalDetailDto> AdditionalDetails,
    List<TicketSelectionDto> Tickets,
    bool IsInvited = false);

public record AdditionalDetailDto(string Name, string Value);

public record TicketSelectionDto(string TicketTypeSlug, int Quantity);
