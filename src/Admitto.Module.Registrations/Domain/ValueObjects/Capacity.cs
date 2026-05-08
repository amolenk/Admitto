using Vogen;

namespace Amolenk.Admitto.Module.Registrations.Domain.ValueObjects;

[ValueObject<int>]
public partial struct Capacity
{
    private const int MinValue = 0;
    private const int MaxValue = 10000;

    private static Validation Validate(int value)
        => value is >= MinValue and <= MaxValue
            ? Validation.Ok
            : Validation.Invalid($"Capacity must be between {MinValue} and {MaxValue}.");
}

