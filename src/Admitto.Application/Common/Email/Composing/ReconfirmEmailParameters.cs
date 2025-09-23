namespace Amolenk.Admitto.Application.Common.Email.Composing;

public record ReconfirmEmailParameters(
    string Recipient,
    string EventName,
    string EventWebsite,
    string FirstName,
    string LastName,
    List<DetailEmailParameter>? Details,
    List<TicketEmailParameter>? Tickets,
    string ReconfirmLink,
    string EditLink,
    string CancelLink)
    : IEmailParameters;