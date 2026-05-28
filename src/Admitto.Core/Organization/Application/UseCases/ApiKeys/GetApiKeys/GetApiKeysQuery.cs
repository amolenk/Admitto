using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Organization.Application.UseCases.ApiKeys.GetApiKeys;

internal sealed record GetApiKeysQuery(Guid TeamId) : Query<IReadOnlyList<ApiKeyListItemDto>>;
