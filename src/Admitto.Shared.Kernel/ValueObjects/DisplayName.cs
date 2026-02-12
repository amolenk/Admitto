using Amolenk.Admitto.Shared.Kernel.ErrorHandling;

namespace Amolenk.Admitto.Shared.Kernel.ValueObjects;

public readonly record struct DisplayName : IStringValueObject
{
    public const int MaxLength = 64;

    public string Value { get; }

    private DisplayName(string value) => Value = value;

    public static ValidationResult<DisplayName> TryFrom(string? value)
        => StringValueObject.TryFrom(
            value,
            MaxLength,
            v => new DisplayName(v));

    public static DisplayName From(string? value) => TryFrom(value).GetValueOrThrow();

    public override string ToString() => Value;
}