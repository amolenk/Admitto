using System.Security.Cryptography;
using System.Text;

namespace Amolenk.Admitto.Module.Shared.Application.Cryptography;

public sealed class SigningService : ISigningService
{
    public string Sign(ReadOnlySpan<byte> payload, ReadOnlySpan<byte> key)
    {
        Span<byte> hash = stackalloc byte[HMACSHA256.HashSizeInBytes];
        HMACSHA256.HashData(key, payload, hash);

        return ToUrlSafeBase64(hash);
    }

    public bool IsValid(ReadOnlySpan<byte> payload, string signature, ReadOnlySpan<byte> key)
    {
        if (string.IsNullOrEmpty(signature))
            return false;

        Span<byte> expectedHash = stackalloc byte[HMACSHA256.HashSizeInBytes];
        HMACSHA256.HashData(key, payload, expectedHash);

        var expectedSignature = ToUrlSafeBase64(expectedHash);
        var expectedBytes = Encoding.ASCII.GetBytes(expectedSignature);
        var actualBytes = Encoding.ASCII.GetBytes(signature);

        return expectedBytes.Length == actualBytes.Length
               && CryptographicOperations.FixedTimeEquals(expectedBytes, actualBytes);
    }

    private static string ToUrlSafeBase64(ReadOnlySpan<byte> bytes) =>
        Convert.ToBase64String(bytes)
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');
}
