namespace Amolenk.Admitto.Module.Organization.Application.UseCases.ApiKeyManagement.GetApiKeys;

public sealed record ApiKeyListItemDto(
    Guid Id,
    string Name,
    string KeyPrefix,
    DateTimeOffset CreatedAt,
    string CreatedBy,
    DateTimeOffset? RevokedAt);
