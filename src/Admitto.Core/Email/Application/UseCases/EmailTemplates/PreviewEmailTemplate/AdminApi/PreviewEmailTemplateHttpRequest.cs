namespace Amolenk.Admitto.Core.Email.Application.UseCases.EmailTemplates.PreviewEmailTemplate.AdminApi;

public record PreviewEmailTemplateHttpRequest(
    string Subject,
    string TextBody,
    string? HtmlBody);
