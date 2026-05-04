namespace Amolenk.Admitto.Module.Organization.Application.UseCases.ApiKeyManagement.CreateApiKey;

internal sealed record CreateApiKeyResult(Guid KeyId, string RawKey, string KeyPrefix);
