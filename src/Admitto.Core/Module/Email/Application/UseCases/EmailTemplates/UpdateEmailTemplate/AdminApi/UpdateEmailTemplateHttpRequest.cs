namespace Amolenk.Admitto.Core.Module.Email.Application.UseCases.EmailTemplates.UpdateEmailTemplate.AdminApi;

public sealed record UpdateEmailTemplateHttpRequest(
    string? Name,
    string Subject,
    string TextBody,
    string? HtmlBody,
    uint Version);
