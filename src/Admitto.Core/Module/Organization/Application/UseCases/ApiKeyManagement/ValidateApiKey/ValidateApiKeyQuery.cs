using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Module.Organization.Application.UseCases.ApiKeyManagement.ValidateApiKey;

internal sealed record ValidateApiKeyQuery(string KeyHash) : Query<Guid?>;
