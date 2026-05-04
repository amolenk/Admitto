using System.Security.Cryptography;
using System.Text;
using Amolenk.Admitto.Module.Organization.Domain.Entities;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Api.Tests.Infrastructure;

internal static class ApiKeyTestHelper
{
    public const string TestRawKey = "test-raw-api-key-abcdefghijklmnopqrstuvwx";
    public const string TestRawKey2 = "test-raw-api-key-2-zyxwvutsrqponmlkjihgfe";

    public static string ComputeHash(string rawKey = TestRawKey)
    {
        var bytes = Encoding.UTF8.GetBytes(rawKey);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    public static ApiKey CreateApiKeyEntity(TeamId teamId) =>
        ApiKey.Create(
            teamId,
            "Test Key",
            TestRawKey[..8],
            ComputeHash(TestRawKey),
            DateTimeOffset.UtcNow,
            "test-setup");

    public static ApiKey CreateApiKeyEntity2(TeamId teamId) =>
        ApiKey.Create(
            teamId,
            "Test Key 2",
            TestRawKey2[..8],
            ComputeHash(TestRawKey2),
            DateTimeOffset.UtcNow,
            "test-setup");
}
