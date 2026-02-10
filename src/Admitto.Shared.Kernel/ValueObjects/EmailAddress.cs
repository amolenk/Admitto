using System.Net.Mail;
using Amolenk.Admitto.Shared.Kernel.ErrorHandling;

namespace Amolenk.Admitto.Shared.Kernel.ValueObjects;

public readonly record struct EmailAddress
{
    public const int MaxLength = 320; // RFC 5321 practical max

    public string Value { get; }

    private EmailAddress(string normalizedValue)
    {
        Value = normalizedValue;
    }
    
    public static ValidationResult<EmailAddress> TryFrom(string? input)
        => NormalizeAndValidate(input)
            .Map(normalized => new EmailAddress(normalized));

    public static EmailAddress From(string input)
        => TryFrom(input).GetValueOrThrow();

    private static ValidationResult<string> NormalizeAndValidate(string? input)
    {
        if (input is null)
            return Errors.Empty;

        var normalized = input.Trim().ToLowerInvariant();

        if (string.IsNullOrWhiteSpace(input))
            return Errors.Empty;
        
        if (normalized.Length > MaxLength)
            return Errors.TooLong;
        
        // Basic but robust validation
        try
        {
            _ = new MailAddress(normalized);
        }
        catch
        {
            return Errors.InvalidFormat;
        }

        return normalized;
    }
    
    private static class Errors
    {
        public static readonly Error Empty = new(
            "email_address.empty",
            "Email is required.");

        public static readonly Error TooLong = new(
            "email_address.too_long",
            $"Email must be at most {MaxLength} character(s).");

        public static readonly Error InvalidFormat = new(
            "email_address.invalid_format",
            $"Email has an invalid format.");
    }
}



