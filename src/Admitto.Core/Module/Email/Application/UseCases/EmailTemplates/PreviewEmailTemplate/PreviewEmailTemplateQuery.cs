using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Module.Email.Application.UseCases.EmailTemplates.PreviewEmailTemplate;

/// <summary>
/// Preview query that takes template content directly, supporting both saved templates and unsaved drafts.
/// </summary>
internal sealed record PreviewEmailTemplateQuery(
    string Subject,
    string TextBody,
    string? HtmlBody) : Query<PreviewEmailTemplateDto>;
