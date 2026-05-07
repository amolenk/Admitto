namespace Amolenk.Admitto.Module.Email.Application.UseCases.EmailTemplates.UpdateEmailTemplate.AdminApi;

public sealed record UpdateEmailTemplateHttpRequest(
    string? Name,
    string Subject,
    string TextBody,
    string? HtmlBody,
    uint Version);
