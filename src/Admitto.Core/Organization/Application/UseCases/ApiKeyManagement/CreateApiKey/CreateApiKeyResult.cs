namespace Amolenk.Admitto.Core.Organization.Application.UseCases.ApiKeyManagement.CreateApiKey;

internal sealed record CreateApiKeyResult(Guid KeyId, string RawKey, string KeyPrefix);
