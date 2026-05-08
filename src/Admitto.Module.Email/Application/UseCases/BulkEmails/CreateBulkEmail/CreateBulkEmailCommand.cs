using Amolenk.Admitto.Module.Email.Domain.ValueObjects;
using Amolenk.Admitto.Module.Shared.Application.Messaging;

namespace Amolenk.Admitto.Module.Email.Application.UseCases.BulkEmails.CreateBulkEmail;

internal sealed record CreateBulkEmailCommand(
    Guid TeamId,
    Guid TicketedEventId,
    string EmailType,
    string? TemplateName,
    string? Subject,
    string? TextBody,
    string? HtmlBody,
    BulkEmailJobSource Source) : Command<Guid>;
