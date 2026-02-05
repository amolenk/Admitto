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
            return Errors.Required();

        var normalized = input.Trim().ToLowerInvariant();

        if (string.IsNullOrWhiteSpace(input))
            return Errors.Empty();
        
        if (normalized.Length > MaxLength)
            return Errors.TooLong(MaxLength);
        
        // Basic but robust validation
        try
        {
            _ = new MailAddress(normalized);
        }
        catch
        {
            return Errors.InvalidFormat();
        }

        return normalized;
    }
    
    private static class Errors
    {
        private const string Name = "email";
    
        public static Error Required() => SharedErrors.ValueObjects.Required(Name);
        public static Error Empty() => SharedErrors.ValueObjects.Empty(Name);
        public static Error TooLong(int max) => SharedErrors.ValueObjects.TooLong(Name, max);
        public static Error InvalidFormat() => SharedErrors.ValueObjects.InvalidFormat(Name);
    }
}



