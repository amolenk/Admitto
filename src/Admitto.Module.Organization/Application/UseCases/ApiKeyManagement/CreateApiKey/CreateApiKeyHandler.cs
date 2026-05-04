using System.Security.Cryptography;
using System.Text;
using Amolenk.Admitto.Module.Organization.Application.Persistence;
using Amolenk.Admitto.Module.Organization.Domain.Entities;
using Amolenk.Admitto.Module.Shared.Application.Messaging;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Module.Organization.Application.UseCases.ApiKeyManagement.CreateApiKey;

internal sealed class CreateApiKeyHandler(IOrganizationWriteStore writeStore)
    : ICommandHandler<CreateApiKeyCommand, CreateApiKeyResult>
{
    public async ValueTask<CreateApiKeyResult> HandleAsync(
        CreateApiKeyCommand command,
        CancellationToken cancellationToken)
    {
        var rawKey = GenerateRawKey();
        var keyPrefix = rawKey[..8];
        var keyHash = ComputeHash(rawKey);

        var apiKey = ApiKey.Create(
            TeamId.From(command.TeamId),
            command.Name,
            keyPrefix,
            keyHash,
            DateTimeOffset.UtcNow,
            command.CreatedBy);

        await writeStore.ApiKeys.AddAsync(apiKey, cancellationToken);

        return new CreateApiKeyResult(apiKey.Id.Value, rawKey, keyPrefix);
    }

    private static string GenerateRawKey()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        return Convert.ToBase64String(bytes)
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');
    }

    private static string ComputeHash(string rawKey)
    {
        var bytes = Encoding.UTF8.GetBytes(rawKey);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
