using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.Json;
using Amolenk.Admitto.Application.Common.Cryptography;

namespace Amolenk.Admitto.Application.Common.Identity;

public record EmailVerifiedToken(string Email, DateTimeOffset VerifiedAtUtc)
{
    private const int ExpirationMinutes = 30;

    public string Encode(ISigningService signingService)
    {
        var payloadJson = JsonSerializer.Serialize(this);
        var signature = signingService.Sign(payloadJson);
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(payloadJson)) + "." + signature;
    }

    public static bool TryDecodeAndValidate(
        string encodedToken,
        ISigningService signingService,
        [MaybeNullWhen(false)] out EmailVerifiedToken token)
    {
        token = null;

        var parts = encodedToken.Split('.', 2);
        if (parts.Length != 2)
        {
            return false;
        }

        var payloadJson = Encoding.UTF8.GetString(Convert.FromBase64String(parts[0]));
        var signature = parts[1];

        if (!signingService.IsValid(payloadJson, signature))
        {
            return false;
        }

        try
        {
            token = JsonSerializer.Deserialize<EmailVerifiedToken>(payloadJson);
            if (token == null)
            {
                return false;
            }
        }
        catch
        {
            return false;
        }

        // Check if token has expired
        if (DateTimeOffset.UtcNow - token.VerifiedAtUtc > TimeSpan.FromMinutes(ExpirationMinutes))
        {
            return false;
        }

        return true;
    }
}