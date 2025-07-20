using Amolenk.Admitto.Domain.ValueObjects;

namespace Amolenk.Admitto.Application.UseCases.Email.SendEmail;

public record SendEmailCommand(
    Guid TeamId,
    Guid TicketedEventId,
    EmailType EmailType,
    Guid DataEntityId,
    string? RecipientEmail = null) : Command;
