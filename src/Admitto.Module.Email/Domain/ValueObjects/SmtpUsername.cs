using Amolenk.Admitto.Module.Shared.Kernel.ErrorHandling;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Module.Email.Domain.ValueObjects;

/// <summary>
/// SMTP authentication username. The aggregate enforces presence when AuthMode = Basic.
/// </summary>
public readonly record struct SmtpUsername : IStringValueObject
{
    public const int MaxLength = 255;

    public string Value { get; }

    private SmtpUsername(string value) => Value = value;

    public static ValidationResult<SmtpUsername> TryFrom(string? value)
        => StringValueObject.TryFrom(
            value,
            MaxLength,
            v => new SmtpUsername(v));

    public static SmtpUsername From(string? value) => TryFrom(value).GetValueOrThrow();

    public override string ToString() => Value;
}
