namespace Amolenk.Admitto.Module.Email.Application.UseCases.EmailTemplates.PreviewEmailTemplate.AdminApi;

public record PreviewDraftEmailTemplateHttpRequest(
    string Subject,
    string TextBody,
    string HtmlBody);
