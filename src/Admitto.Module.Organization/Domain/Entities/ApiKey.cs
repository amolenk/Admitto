using Amolenk.Admitto.Module.Organization.Domain.ValueObjects;
using Amolenk.Admitto.Module.Shared.Kernel.Entities;
using Amolenk.Admitto.Module.Shared.Kernel.ErrorHandling;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Module.Organization.Domain.Entities;

public class ApiKey : Entity<ApiKeyId>
{
    // ReSharper disable once UnusedMember.Local
    // Required for EF Core
    private ApiKey()
    {
    }

    private ApiKey(
        ApiKeyId id,
        TeamId teamId,
        string name,
        string keyPrefix,
        string keyHash,
        DateTimeOffset createdAt,
        string createdBy)
        : base(id)
    {
        TeamId = teamId;
        Name = name;
        KeyPrefix = keyPrefix;
        KeyHash = keyHash;
        CreatedAt = createdAt;
        CreatedBy = createdBy;
    }

    public TeamId TeamId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string KeyPrefix { get; private set; } = string.Empty;
    public string KeyHash { get; private set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; private set; }
    public string CreatedBy { get; private set; } = string.Empty;
    public DateTimeOffset? RevokedAt { get; private set; }

    public bool IsActive => RevokedAt is null;

    public static ApiKey Create(
        TeamId teamId,
        string name,
        string keyPrefix,
        string keyHash,
        DateTimeOffset createdAt,
        string createdBy)
        => new(ApiKeyId.New(), teamId, name, keyPrefix, keyHash, createdAt, createdBy);

    public void Revoke(DateTimeOffset revokedAt)
    {
        if (!IsActive)
        {
            throw new BusinessRuleViolationException(Errors.ApiKeyAlreadyRevoked(Id));
        }

        RevokedAt = revokedAt;
    }

    internal static class Errors
    {
        public static Error ApiKeyAlreadyRevoked(ApiKeyId keyId) =>
            new(
                "api-key.already_revoked",
                "The API key is already revoked.",
                Details: new Dictionary<string, object?>
                {
                    ["keyId"] = keyId.Value
                },
                Type: ErrorType.Conflict);
    }
}
