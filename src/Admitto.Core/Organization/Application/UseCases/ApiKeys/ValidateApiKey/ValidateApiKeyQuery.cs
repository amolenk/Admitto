using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Organization.Application.UseCases.ApiKeys.ValidateApiKey;

internal sealed record ValidateApiKeyQuery(string KeyHash) : Query<Guid?>;
