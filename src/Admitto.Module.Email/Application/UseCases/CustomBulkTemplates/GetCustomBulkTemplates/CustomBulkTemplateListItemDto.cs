namespace Amolenk.Admitto.Module.Email.Application.UseCases.CustomBulkTemplates.GetCustomBulkTemplates;

public sealed record CustomBulkTemplateListItemDto(
    Guid Id,
    string Name,
    string Subject,
    DateTimeOffset CreatedAt,
    DateTimeOffset LastChangedAt,
    uint Version);
