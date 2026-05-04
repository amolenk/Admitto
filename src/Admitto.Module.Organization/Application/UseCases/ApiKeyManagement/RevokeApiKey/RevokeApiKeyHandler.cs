using Amolenk.Admitto.Module.Organization.Application.Persistence;
using Amolenk.Admitto.Module.Organization.Domain.Entities;
using Amolenk.Admitto.Module.Organization.Domain.ValueObjects;
using Amolenk.Admitto.Module.Shared.Application.Messaging;
using Amolenk.Admitto.Module.Shared.Kernel.ErrorHandling;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Module.Organization.Application.UseCases.ApiKeyManagement.RevokeApiKey;

internal sealed class RevokeApiKeyHandler(IOrganizationWriteStore writeStore)
    : ICommandHandler<RevokeApiKeyCommand>
{
    public async ValueTask HandleAsync(RevokeApiKeyCommand command, CancellationToken cancellationToken)
    {
        var teamId = TeamId.From(command.TeamId);
        var keyId = ApiKeyId.From(command.KeyId);

        var apiKey = await writeStore.ApiKeys
            .FirstOrDefaultAsync(
                k => k.Id == keyId && k.TeamId == teamId,
                cancellationToken)
            ?? throw new BusinessRuleViolationException(NotFoundError.Create<ApiKey>(command.KeyId));

        apiKey.Revoke(DateTimeOffset.UtcNow);
    }
}
