using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Amolenk.Admitto.Module.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;
using Microsoft.Extensions.Options;

namespace Amolenk.Admitto.Module.Registrations.Application.Security;

internal sealed class HmacVerificationTokenService(
    IOptions<VerificationTokenOptions> options,
    TimeProvider timeProvider) : IVerificationTokenService
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public string Issue(EmailAddress email, TicketedEventId eventId, TeamId teamId)
    {
        var header = Base64UrlEncode(JsonSerializer.SerializeToUtf8Bytes(new
        {
            alg = "HS256",
            typ = "JWT"
        }, SerializerOptions));

        var issuedAt = timeProvider.GetUtcNow();
        var expiresAt = issuedAt.AddMinutes(options.Value.TokenTtlMinutes);

        var payload = Base64UrlEncode(JsonSerializer.SerializeToUtf8Bytes(new
        {
            sub = email.Value,
            eid = eventId.Value,
            tid = teamId.Value,
            iat = issuedAt.ToUnixTimeSeconds(),
            exp = expiresAt.ToUnixTimeSeconds()
        }, SerializerOptions));

        var signingInput = $"{header}.{payload}";
        var signature = ComputeSignature(signingInput);

        return $"{signingInput}.{signature}";
    }

    public VerificationTokenClaims? Validate(string token, TicketedEventId eventId)
    {
        var parts = token.Split('.');
        if (parts.Length != 3)
            return null;

        var signingInput = $"{parts[0]}.{parts[1]}";
        var expectedSignature = ComputeSignature(signingInput);

        if (!CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(expectedSignature),
                Encoding.UTF8.GetBytes(parts[2])))
            return null;

        JsonElement claims;
        try
        {
            var payloadBytes = Base64UrlDecode(parts[1]);
            claims = JsonSerializer.Deserialize<JsonElement>(payloadBytes, SerializerOptions);
        }
        catch
        {
            return null;
        }

        if (!claims.TryGetProperty("exp", out var expProp) ||
            !expProp.TryGetInt64(out var exp) ||
            DateTimeOffset.FromUnixTimeSeconds(exp) <= timeProvider.GetUtcNow())
            return null;

        if (!claims.TryGetProperty("eid", out var eidProp) ||
            !eidProp.TryGetGuid(out var claimsEventId) ||
            claimsEventId != eventId.Value)
            return null;

        if (!claims.TryGetProperty("sub", out var subProp))
            return null;

        var emailValue = subProp.GetString();
        if (string.IsNullOrWhiteSpace(emailValue))
            return null;

        var emailResult = EmailAddress.TryFrom(emailValue);
        if (!emailResult.IsSuccess)
            return null;

        return new VerificationTokenClaims(emailResult.Value);
    }

    private string ComputeSignature(string input)
    {
        var keyBytes = Convert.FromBase64String(options.Value.SigningKey);
        var inputBytes = Encoding.UTF8.GetBytes(input);
        var hashBytes = HMACSHA256.HashData(keyBytes, inputBytes);
        return Base64UrlEncode(hashBytes);
    }

    private static string Base64UrlEncode(byte[] bytes)
        => Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');

    private static byte[] Base64UrlDecode(string value)
    {
        var padded = value
            .Replace('-', '+')
            .Replace('_', '/');
        padded = (padded.Length % 4) switch
        {
            2 => padded + "==",
            3 => padded + "=",
            _ => padded
        };
        return Convert.FromBase64String(padded);
    }
}
