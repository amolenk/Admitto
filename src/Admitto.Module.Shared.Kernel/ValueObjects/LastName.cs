using Amolenk.Admitto.Module.Shared.Kernel.ErrorHandling;

namespace Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;

public readonly record struct LastName : IStringValueObject
{
    public const int MaxLength = 100;

    public string Value { get; }

    private LastName(string value) => Value = value;

    public static ValidationResult<LastName> TryFrom(string? value)
        => StringValueObject.TryFrom(
            value,
            MaxLength,
            v => new LastName(v));

    public static LastName From(string? value) => TryFrom(value).GetValueOrThrow();

    public override string ToString() => Value;
}
