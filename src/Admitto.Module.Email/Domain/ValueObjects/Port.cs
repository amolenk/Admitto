using Vogen;

namespace Amolenk.Admitto.Module.Email.Domain.ValueObjects;

/// <summary>
/// TCP/UDP port number (1–65535).
/// </summary>
[ValueObject<int>]
public partial struct Port
{
    public const int MinValue = 1;
    public const int MaxValue = 65_535;

    private static Validation Validate(int value)
        => value is >= MinValue and <= MaxValue
            ? Validation.Ok
            : Validation.Invalid($"Port must be between {MinValue} and {MaxValue}.");
}

