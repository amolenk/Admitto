namespace Amolenk.Admitto.Core.Organization.Application.UseCases.ApiKeys.CreateApiKey;

internal sealed record CreateApiKeyResult(Guid KeyId, string RawKey, string KeyPrefix);
