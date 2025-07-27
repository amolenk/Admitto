namespace Amolenk.Admitto.Application.UseCases.Email.TestEmail;

public record TestEmailRequest(
    string Recipient,
    List<AdditionalDetailDto>? AdditionalDetails = null,
    List<TicketSelectionDto>? Tickets = null);

public record AdditionalDetailDto(string Name, string Value);

public record TicketSelectionDto(string TicketTypeSlug, int Quantity);