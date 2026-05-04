namespace Amolenk.Admitto.Module.Registrations.Application.Security;

internal sealed class VerificationTokenOptions
{
    public const string SectionName = "Registrations:VerificationToken";

    /// <summary>Base64-encoded 32-byte HMAC-SHA256 signing key.</summary>
    public string SigningKey { get; set; } = null!;

    /// <summary>Token lifetime in minutes. Defaults to 15.</summary>
    public int TokenTtlMinutes { get; set; } = 15;
}
