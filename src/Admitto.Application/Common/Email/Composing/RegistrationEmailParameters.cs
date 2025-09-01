using Amolenk.Admitto.Domain.ValueObjects;

namespace Amolenk.Admitto.Application.Common.Email.Composing;

public record RegistrationEmailParameters(
    string EventName,
    string EventWebsite,
    string Recipient,
    EmailRecipientType RecipientType,
    string FirstName,
    string LastName,
    List<DetailEmailParameter>? Details,
    List<TicketEmailParameter>? Tickets,
    string QRCodeLink,
    string ReconfirmLink,
    string CancelLink)
    : IEmailParameters;
    
public record DetailEmailParameter(string Name, string Value);

public record TicketEmailParameter(string Name, int Quantity);