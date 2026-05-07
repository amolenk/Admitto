using Amolenk.Admitto.Module.Email.Domain.ValueObjects;
using Amolenk.Admitto.Module.Shared.Application.Messaging;

namespace Amolenk.Admitto.Module.Email.Application.UseCases.EmailTemplates.UpdateEmailTemplate;

internal sealed record UpdateEmailTemplateCommand(
    EmailTemplateId Id,
    string? Name,
    string Subject,
    string TextBody,
    string? HtmlBody,
    uint Version) : Command;
