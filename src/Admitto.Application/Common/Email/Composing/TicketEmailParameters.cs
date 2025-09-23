namespace Amolenk.Admitto.Application.Common.Email.Composing;

public record TicketEmailParameters(
    string Recipient,
    string EventName,
    string EventWebsite,
    string FirstName,
    string LastName,
    List<DetailEmailParameter>? Details,
    List<TicketEmailParameter>? Tickets,
    string QRCodeLink,
    string EditLink,
    string CancelLink)
    : IEmailParameters;
    
public record DetailEmailParameter(string Name, string Value);

public record TicketEmailParameter(string Slug, string Name, string[] SlotNames, int Quantity);