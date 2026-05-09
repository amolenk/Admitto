using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Email.Application.UseCases.EmailTemplates.UpdateEmailTemplate;

internal sealed record UpdateEmailTemplateCommand(
    Guid Id,
    string? Name,
    string Subject,
    string TextBody,
    string? HtmlBody,
    uint Version) : Command;
