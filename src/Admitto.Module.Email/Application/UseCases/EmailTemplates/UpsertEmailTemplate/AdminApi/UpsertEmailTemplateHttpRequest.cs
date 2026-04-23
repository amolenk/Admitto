using Amolenk.Admitto.Module.Email.Domain.ValueObjects;

namespace Amolenk.Admitto.Module.Email.Application.UseCases.EmailTemplates.UpsertEmailTemplate.AdminApi;

/// <summary>
/// PUT request body for upserting an email template. When <see cref="Version"/> is omitted the
/// endpoint creates a new template; when supplied it updates the existing template using optimistic
/// concurrency.
/// </summary>
public sealed record UpsertEmailTemplateHttpRequest(
    string Subject,
    string TextBody,
    string HtmlBody,
    uint? Version)
{
    internal UpsertEmailTemplateCommand ToCommand(EmailSettingsScope scope, Guid scopeId, string type) =>
        new(scope, scopeId, type, Subject, TextBody, HtmlBody, Version);
}
