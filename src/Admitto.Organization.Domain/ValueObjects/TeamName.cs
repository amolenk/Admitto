using Amolenk.Admitto.Shared.Kernel.ErrorHandling;
using Amolenk.Admitto.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Organization.Domain.ValueObjects;

public readonly record struct TeamName : IStringValueObject
{
    private const int MaxLength = 100;

    public string Value { get; }

    private TeamName(string value) => Value = value;

    public static ValidationResult<TeamName> TryFrom(string? value)
        => StringValueObject.TryFrom(
            value,
            MaxLength,
            v => new TeamName(v));

    public static TeamName From(string? value) => TryFrom(value).GetValueOrThrow();

    public override string ToString() => Value;
}