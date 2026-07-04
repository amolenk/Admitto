using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Email.Application.UseCases.Emails.SendEmail;

public sealed record SendEmailCommand(
    Guid TeamId,
    Guid TicketedEventId,
    string RecipientAddress,
    string RecipientName,
    string EmailType,
    string IdempotencyKey,
    object Parameters,
    Guid? RegistrationId = null) : Command;
