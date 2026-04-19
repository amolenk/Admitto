using Amolenk.Admitto.Module.Shared.Kernel.ErrorHandling;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Module.Email.Domain.ValueObjects;

/// <summary>
/// SMTP server hostname. Permissive: non-empty, trimmed, length-capped — no DNS or RFC parsing.
/// </summary>
public readonly record struct Hostname : IStringValueObject
{
    public const int MaxLength = 255;

    public string Value { get; }

    private Hostname(string value) => Value = value;

    public static ValidationResult<Hostname> TryFrom(string? value)
        => StringValueObject.TryFrom(
            value,
            MaxLength,
            v => new Hostname(v));

    public static Hostname From(string? value) => TryFrom(value).GetValueOrThrow();

    public override string ToString() => Value;
}
