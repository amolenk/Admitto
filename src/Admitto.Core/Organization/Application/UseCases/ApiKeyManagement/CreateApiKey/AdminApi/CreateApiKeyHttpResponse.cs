namespace Amolenk.Admitto.Core.Organization.Application.UseCases.ApiKeyManagement.CreateApiKey.AdminApi;

public sealed record CreateApiKeyHttpResponse(Guid Id, string Name, string KeyPrefix, string Key);
