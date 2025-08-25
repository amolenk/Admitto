namespace Amolenk.Admitto.Application.UseCases.Registrations.Invite;

public record InviteRequest(
    string Email,
    string FirstName,
    string LastName,
    List<AdditionalDetailDto> AdditionalDetails,
    List<TicketSelectionDto> Tickets);

public record AdditionalDetailDto(string Name, string Value);

public record TicketSelectionDto(string TicketTypeSlug, int Quantity);
