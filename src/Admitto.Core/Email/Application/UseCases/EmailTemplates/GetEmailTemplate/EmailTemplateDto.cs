namespace Amolenk.Admitto.Core.Email.Application.UseCases.EmailTemplates.GetEmailTemplate;

public sealed record EmailTemplateDto(
    Guid Id,
    string Name,
    string Kind,
    string? Description,
    string Subject,
    string TextBody,
    string? HtmlBody,
    bool IsCustomised,
    uint Version);

