using System.Text;
using System.Text.Json;
using Amolenk.Admitto.Application.Common.Cryptography;
using Amolenk.Admitto.Domain.ValueObjects;

namespace Amolenk.Admitto.Application.Common.Email.Verification;

public record EmailVerifiedToken(string Email, DateTimeOffset ExpiresAtUtc, List<Coupon>? Coupons = null)
{
    public async ValueTask<string> EncodeAsync(
        ISigningService signingService,
        Guid ticketedEventId,
        CancellationToken cancellationToken = default)
    {
        var payloadJson = JsonSerializer.Serialize(this);
        var signature = await signingService.SignAsync(payloadJson, ticketedEventId, cancellationToken);
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(payloadJson)) + "." + signature;
    }

    public static async ValueTask<EmailVerifiedToken?> TryDecodeAndValidateAsync(
        string encodedToken,
        Guid ticketedEventId,
        ISigningService signingService,
        CancellationToken cancellationToken = default)
    {
        EmailVerifiedToken? token = null;

        var parts = encodedToken.Split('.', 2);
        if (parts.Length != 2)
        {
            return token;
        }

        var payloadJson = Encoding.UTF8.GetString(Convert.FromBase64String(parts[0]));
        var signature = parts[1];

        if (!await signingService.IsValidAsync(payloadJson, signature, ticketedEventId, cancellationToken))
        {
            return token;
        }
        
        try
        {
            token = JsonSerializer.Deserialize<EmailVerifiedToken>(payloadJson);
            if (token is null)
            {
                return null;
            }
        }
        catch
        {
            return null;
        }

        // Check if token has expired
        return token.ExpiresAtUtc > DateTimeOffset.UtcNow ? token : null;
    }
}