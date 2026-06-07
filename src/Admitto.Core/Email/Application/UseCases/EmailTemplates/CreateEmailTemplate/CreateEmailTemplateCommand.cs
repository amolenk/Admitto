using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Email.Application.UseCases.EmailTemplates.CreateEmailTemplate;

internal sealed record CreateEmailTemplateCommand(
    Guid TeamId,
    Guid? TicketedEventId,
    string Name,
    string? Subject,
    string? TextBody,
    string? HtmlBody) : Command<Guid>;
