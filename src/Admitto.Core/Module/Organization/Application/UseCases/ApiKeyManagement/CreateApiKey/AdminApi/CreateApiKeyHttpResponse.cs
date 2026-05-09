namespace Amolenk.Admitto.Core.Module.Organization.Application.UseCases.ApiKeyManagement.CreateApiKey.AdminApi;

public sealed record CreateApiKeyHttpResponse(Guid Id, string Name, string KeyPrefix, string Key);
