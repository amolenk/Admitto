using System.Security.Cryptography;
using Vogen;

namespace Amolenk.Admitto.Core.Registrations.Domain.ValueObjects;

/// <summary>
/// A high-entropy opaque bearer secret for a shared scanner link. It is never derived from,
/// nor interchangeable with, a <see cref="Amolenk.Admitto.Core.Registrations.Domain.Entities.Registration"/> ID.
/// </summary>
[ValueObject<string>]
public partial struct ScannerLinkSecret
{
    private static Validation Validate(string value)
        => !string.IsNullOrWhiteSpace(value)
            ? Validation.Ok
            : Validation.Invalid("Scanner link secret cannot be empty.");

    /// <summary>
    /// Generates a new cryptographically random, URL-safe secret.
    /// </summary>
    public static ScannerLinkSecret New()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        var token = Convert.ToBase64String(bytes)
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');

        return From(token);
    }
}
