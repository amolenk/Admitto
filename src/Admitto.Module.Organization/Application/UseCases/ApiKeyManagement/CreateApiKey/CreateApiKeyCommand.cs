using Amolenk.Admitto.Module.Shared.Application.Messaging;

namespace Amolenk.Admitto.Module.Organization.Application.UseCases.ApiKeyManagement.CreateApiKey;

internal sealed record CreateApiKeyCommand(
    Guid TeamId,
    string Name,
    string CreatedBy)
    : Command<CreateApiKeyResult>;
