namespace Amolenk.Admitto.Module.Email.Application.UseCases.EmailTemplates.GetEmailTemplates;

public sealed record EmailTemplateListItemDto(
    Guid? Id,
    string Name,
    string Kind,
    string? Description,
    string Subject,
    bool IsCustomised,
    uint? Version);
