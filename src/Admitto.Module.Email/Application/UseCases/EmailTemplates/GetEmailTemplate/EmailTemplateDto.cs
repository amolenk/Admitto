namespace Amolenk.Admitto.Module.Email.Application.UseCases.EmailTemplates.GetEmailTemplate;

public sealed record EmailTemplateDto(
    string Subject,
    string TextBody,
    string? HtmlBody,
    bool IsCustom,
    uint? Version);
