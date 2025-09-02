namespace Amolenk.Admitto.Application.UseCases.Email.SendEmail;

/// <summary>
/// Represents a command to send a single email.
/// </summary>
public record SendEmailCommand(
    Guid TicketedEventId,
    Guid DataEntityId,
    string EmailType,
    Guid? TeamId = null,
    Dictionary<string, string>? AdditionalParameters = null)
    : Command;
