using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Organization.Application.UseCases.ApiKeys.RevokeApiKey;

internal sealed record RevokeApiKeyCommand(Guid TeamId, Guid KeyId) : Command;
