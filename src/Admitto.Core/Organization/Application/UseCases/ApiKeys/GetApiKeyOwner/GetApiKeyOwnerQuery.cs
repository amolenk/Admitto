using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Organization.Application.UseCases.ApiKeys.GetApiKeyOwner;

internal sealed record GetApiKeyOwnerQuery(string KeyHash) : Query<Guid?>;
