namespace Amolenk.Admitto.Core.Module.Email.Application.UseCases.EmailTemplates.PreviewEmailTemplate;

public sealed record PreviewEmailTemplateDto(
    string RenderedSubject,
    string RenderedTextBody,
    string RenderedHtmlBody);
