using Amolenk.Admitto.Module.Shared.Application.Messaging;

namespace Amolenk.Admitto.Module.Organization.Application.UseCases.ApiKeyManagement.RevokeApiKey;

internal sealed record RevokeApiKeyCommand(Guid TeamId, Guid KeyId) : Command;
