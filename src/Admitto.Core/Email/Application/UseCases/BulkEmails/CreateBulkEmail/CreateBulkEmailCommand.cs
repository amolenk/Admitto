using Amolenk.Admitto.Core.Email.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Email.Application.UseCases.BulkEmails.CreateBulkEmail;

internal sealed record CreateBulkEmailCommand(
    Guid TeamId,
    Guid TicketedEventId,
    string EmailType,
    string Subject,
    string TextBody,
    string HtmlBody,
    BulkEmailJobSource Source) : Command<Guid>;
