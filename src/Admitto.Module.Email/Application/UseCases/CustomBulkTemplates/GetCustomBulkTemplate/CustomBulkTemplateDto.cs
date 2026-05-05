namespace Amolenk.Admitto.Module.Email.Application.UseCases.CustomBulkTemplates.GetCustomBulkTemplate;

public sealed record CustomBulkTemplateDto(
    Guid Id,
    string Name,
    string Subject,
    string TextBody,
    string? HtmlBody,
    DateTimeOffset CreatedAt,
    DateTimeOffset LastChangedAt,
    uint Version);
