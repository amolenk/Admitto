using Amolenk.Admitto.Core.Module.Organization.Application.Persistence;
using Amolenk.Admitto.Core.Module.Organization.Domain.Entities;
using Amolenk.Admitto.Core.Module.Organization.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Amolenk.Admitto.Core.Shared.Kernel.ErrorHandling;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Core.Module.Organization.Application.UseCases.ApiKeyManagement.RevokeApiKey;

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
