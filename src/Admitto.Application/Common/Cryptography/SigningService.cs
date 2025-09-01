using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Caching.Memory;

namespace Amolenk.Admitto.Application.Common.Cryptography;

public interface ISigningService
{
    ValueTask<string> SignAsync(Guid value, Guid ticketedEventId, CancellationToken cancellationToken = default);
    ValueTask<string> SignAsync(string value, Guid ticketedEventId, CancellationToken cancellationToken = default);

    ValueTask<bool> IsValidAsync(
        Guid value,
        string signature,
        Guid ticketedEventId,
        CancellationToken cancellationToken = default);

    ValueTask<bool> IsValidAsync(
        string value,
        string signature,
        Guid ticketedEventId,
        CancellationToken cancellationToken = default);
}

public class SigningService(
    IApplicationContext applicationContext,
    IMemoryCache memoryCache)
    : ISigningService
{
    public ValueTask<string> SignAsync(
        Guid value,
        Guid ticketedEventId,
        CancellationToken cancellationToken = default) =>
        SignAsync(value.ToString(), ticketedEventId, cancellationToken);

    public async ValueTask<string> SignAsync(
        string value,
        Guid ticketedEventId,
        CancellationToken cancellationToken = default)
    {
        var signingKey = await GetSigningKeyAsync(ticketedEventId, cancellationToken);

        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(signingKey));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(value));
        // Ensure the signature is URL-safe
        return Convert.ToBase64String(hash).Replace("+", "-").Replace("/", "_").Replace("=", "");
    }

    public ValueTask<bool> IsValidAsync(
        Guid value,
        string signature,
        Guid ticketedEventId,
        CancellationToken cancellationToken = default)
        => IsValidAsync(value.ToString(), signature, ticketedEventId, cancellationToken);


    public async ValueTask<bool> IsValidAsync(
        string value,
        string signature,
        Guid ticketedEventId,
        CancellationToken cancellationToken = default)
    {
        var signingKey = await GetSigningKeyAsync(ticketedEventId, cancellationToken);

        var expectedSignature = await SignAsync(value, ticketedEventId, cancellationToken);
        return TimingSafeEquals(expectedSignature, signature);
    }

    /// <summary>
    /// Prevents timing attacks by comparing two strings in constant time.
    /// </summary>
    private static bool TimingSafeEquals(string a, string b)
    {
        var aBytes = Encoding.UTF8.GetBytes(a);
        var bBytes = Encoding.UTF8.GetBytes(b);

        return aBytes.Length == bBytes.Length
               && CryptographicOperations.FixedTimeEquals(aBytes, bBytes);
    }

    private async ValueTask<string> GetSigningKeyAsync(Guid eventId, CancellationToken cancellationToken)
    {
        var cacheKey = $"skey:{eventId}";

        if (memoryCache.TryGetValue(cacheKey, out string? key))
        {
            return key!;
        }

        var signingKey = await applicationContext.TicketedEvents
            .AsNoTracking()
            .Where(e => e.Id == eventId)
            .Select(e => e.SigningKey)
            .FirstOrDefaultAsync(cancellationToken);

        if (signingKey is null)
        {
            throw new ApplicationRuleException(ApplicationRuleError.TicketedEvent.NotFound);
        }

        memoryCache.Set(cacheKey, signingKey);
        return signingKey;
    }
}