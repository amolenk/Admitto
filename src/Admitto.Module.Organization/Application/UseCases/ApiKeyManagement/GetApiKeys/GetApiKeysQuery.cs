using Amolenk.Admitto.Module.Shared.Application.Messaging;

namespace Amolenk.Admitto.Module.Organization.Application.UseCases.ApiKeyManagement.GetApiKeys;

internal sealed record GetApiKeysQuery(Guid TeamId) : Query<IReadOnlyList<ApiKeyListItemDto>>;
