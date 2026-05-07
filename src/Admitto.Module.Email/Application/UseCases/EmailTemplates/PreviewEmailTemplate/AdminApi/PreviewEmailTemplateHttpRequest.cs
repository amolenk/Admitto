namespace Amolenk.Admitto.Module.Email.Application.UseCases.EmailTemplates.PreviewEmailTemplate.AdminApi;

public record PreviewEmailTemplateHttpRequest(
    string Subject,
    string TextBody,
    string? HtmlBody);
