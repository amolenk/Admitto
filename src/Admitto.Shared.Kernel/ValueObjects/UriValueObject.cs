using Amolenk.Admitto.Shared.Kernel.ErrorHandling;

namespace Amolenk.Admitto.Shared.Kernel.ValueObjects;

public interface IUriValueObject
{
    Uri Value { get; }
}

public static class UriValueObject
{
    // The practical maximum length for a URI seems to be around 2000 characters,
    // but most URIs are much shorter. Setting a limit of 320 characters
    // to cover most use cases while preventing excessively long URIs.
    // This aligns with the maximum length for email addresses defined in RFC 5321.
    private const int MaxLength = 320;

    public static ValidationResult<TValueObject> TryFrom<TValueObject>(
        string? value,
        Func<Uri, TValueObject> factory,
        Error emptyError,
        Error tooLongError,
        Error invalidFormatError)
        where TValueObject : IUriValueObject
    {
        if (string.IsNullOrWhiteSpace(value))
            return emptyError;

        var normalized = value.Trim();

        if (normalized.Length > MaxLength) return tooLongError;

        return !Uri.TryCreate(normalized, UriKind.Absolute, out var uri)
            ? invalidFormatError
            : ValidationResult<TValueObject>.Success(factory(uri));
    }
}