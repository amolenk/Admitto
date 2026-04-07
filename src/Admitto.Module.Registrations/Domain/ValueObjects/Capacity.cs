using Amolenk.Admitto.Module.Shared.Kernel.ErrorHandling;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Module.Registrations.Domain.ValueObjects;

public readonly record struct Capacity : IInt32ValueObject
{
    private const int MinValue = 0;
    private const int MaxValue = 10000;

    public int Value { get; }

    private Capacity(int value) => Value = value;

    public static ValidationResult<Capacity> TryFrom(int value)
        => Int32ValueObject.TryFrom(value, MinValue, MaxValue, v => new Capacity(v));

    public static Capacity From(int value) => TryFrom(value).GetValueOrThrow();

    public override string ToString() => Value.ToString();
}
