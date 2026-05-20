using Amolenk.Admitto.Core.Organization.Application.Persistence;
using Amolenk.Admitto.Core.Organization.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Amolenk.Admitto.Core.Shared.Application.Persistence;

namespace Amolenk.Admitto.Core.Organization.Application.UseCases.ApiKeyManagement.RevokeApiKey;

internal sealed class RevokeApiKeyHandler(IOrganizationWriteStore writeStore)
    : ICommandHandler<RevokeApiKeyCommand>
{
    public async ValueTask HandleAsync(RevokeApiKeyCommand command, CancellationToken cancellationToken)
    {
        var teamId = TeamId.From(command.TeamId);
        var keyId = ApiKeyId.From(command.KeyId);

        var apiKey = await writeStore.ApiKeys.GetAsync(
                 k => k.Id == keyId && k.TeamId == teamId,
                 cancellationToken);

        apiKey.Revoke(DateTimeOffset.UtcNow);
    }
}
