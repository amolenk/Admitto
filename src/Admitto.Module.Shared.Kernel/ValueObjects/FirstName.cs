using Amolenk.Admitto.Module.Shared.Kernel.ErrorHandling;

namespace Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;

public readonly record struct FirstName : IStringValueObject
{
    public const int MaxLength = 100;

    public string Value { get; }

    private FirstName(string value) => Value = value;

    public static ValidationResult<FirstName> TryFrom(string? value)
        => StringValueObject.TryFrom(
            value,
            MaxLength,
            v => new FirstName(v));

    public static FirstName From(string? value) => TryFrom(value).GetValueOrThrow();

    public override string ToString() => Value;
}
