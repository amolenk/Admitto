using Amolenk.Admitto.Module.Shared.Application.Messaging;

namespace Amolenk.Admitto.Module.Organization.Application.UseCases.ApiKeyManagement.ValidateApiKey;

internal sealed record ValidateApiKeyQuery(string KeyHash) : Query<Guid?>;
