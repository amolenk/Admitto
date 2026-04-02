using Amolenk.Admitto.Module.Shared.Kernel.ErrorHandling;

namespace Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;

public interface IStringValueObject
{
    string Value { get; }
}

public static class StringValueObject
{
    public static ValidationResult<TValueObject> TryFrom<TValueObject>(
        string? value,
        int maxLength,
        Func<string, TValueObject> factory)
        where TValueObject : IStringValueObject
    {
        if (string.IsNullOrWhiteSpace(value))
            return CommonErrors.TextEmpty;

        var normalized = value.Trim();

        if (normalized.Length > maxLength) return CommonErrors.TextTooLong(maxLength);

        return ValidationResult<TValueObject>.Success(factory(normalized));
    }

}
