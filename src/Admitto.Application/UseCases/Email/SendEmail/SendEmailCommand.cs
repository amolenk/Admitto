using Amolenk.Admitto.Domain.ValueObjects;

namespace Amolenk.Admitto.Application.UseCases.Email.SendEmail;

public record SendEmailCommand(
    Guid TeamId,
    Guid TicketedEventId,
    Guid DataEntityId,
    EmailType EmailType,
    string? RecipientEmail = null)
    : Command;
