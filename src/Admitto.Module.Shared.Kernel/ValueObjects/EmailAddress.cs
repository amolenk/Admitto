using System.Net.Mail;
using Vogen;

namespace Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;

[ValueObject<string>]
public partial struct EmailAddress
{
    public const int MaxLength = 320; // RFC 5321 practical max

    private static string NormalizeInput(string value) => value.Trim().ToLowerInvariant();

    private static Validation Validate(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return Validation.Invalid("Email is required.");
        if (value.Length > MaxLength)
            return Validation.Invalid($"Email must be at most {MaxLength} character(s).");
        try { _ = new MailAddress(value); }
        catch { return Validation.Invalid("Email has an invalid format."); }
        return Validation.Ok;
    }
}

