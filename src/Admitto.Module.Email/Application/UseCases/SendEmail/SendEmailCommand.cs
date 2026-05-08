using Amolenk.Admitto.Module.Shared.Application.Messaging;

namespace Amolenk.Admitto.Module.Email.Application.UseCases.SendEmail;

public sealed record SendEmailCommand(
    Guid TeamId,
    Guid TicketedEventId,
    string RecipientAddress,
    string RecipientName,
    string EmailType,
    string IdempotencyKey,
    object Parameters,
    Guid? RegistrationId = null) : Command;
