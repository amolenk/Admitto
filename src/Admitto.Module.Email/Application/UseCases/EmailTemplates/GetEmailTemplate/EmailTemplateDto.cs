namespace Amolenk.Admitto.Module.Email.Application.UseCases.EmailTemplates.GetEmailTemplate;

public sealed record EmailTemplateDto(
    string Subject,
    string TextBody,
    string HtmlBody,
    uint Version);
