using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Organization.Application.UseCases.ApiKeyManagement.CreateApiKey;

internal sealed record CreateApiKeyCommand(
    Guid TeamId,
    string Name,
    string CreatedBy)
    : Command<CreateApiKeyResult>;
