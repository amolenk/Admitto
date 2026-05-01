namespace Amolenk.Admitto.Module.Shared.Application.Cryptography;

/// <summary>
/// Stateless HMAC-SHA256 signing primitive. Callers pass the key explicitly
/// at every call so the service can be reused across modules and per-event
/// keying schemes without coupling to a particular key store.
/// </summary>
public interface ISigningService
{
    /// <summary>
    /// Produces a URL-safe Base64 (no <c>+</c>, <c>/</c>, or <c>=</c>) HMAC-SHA256
    /// signature of <paramref name="payload"/> under <paramref name="key"/>.
    /// </summary>
    string Sign(ReadOnlySpan<byte> payload, ReadOnlySpan<byte> key);

    /// <summary>
    /// Verifies <paramref name="signature"/> against <paramref name="payload"/>
    /// under <paramref name="key"/> using a fixed-time comparison.
    /// </summary>
    bool IsValid(ReadOnlySpan<byte> payload, string signature, ReadOnlySpan<byte> key);
}
