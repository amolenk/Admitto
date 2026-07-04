using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Email.Application.UseCases.Emails.DeliverEmail;

public sealed record DeliverEmailCommand(
    Guid TeamId,
    Guid TicketedEventId,
    string RecipientAddress,
    string RecipientName,
    string EmailType,
    string IdempotencyKey,
    string Subject,
    string TextBody,
    string HtmlBody) : Command;
