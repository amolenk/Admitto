using Amolenk.Admitto.Module.Shared.Kernel.ErrorHandling;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Module.Email.Domain.ValueObjects;

/// <summary>
/// TCP/UDP port number (1–65535).
/// </summary>
public readonly record struct Port : IInt32ValueObject
{
    public const int MinValue = 1;
    public const int MaxValue = 65_535;

    public int Value { get; }

    private Port(int value) => Value = value;

    public static ValidationResult<Port> TryFrom(int value)
        => Int32ValueObject.TryFrom(
            value,
            MinValue,
            MaxValue,
            v => new Port(v));

    public static Port From(int value) => TryFrom(value).GetValueOrThrow();

    public override string ToString() => Value.ToString();
}
