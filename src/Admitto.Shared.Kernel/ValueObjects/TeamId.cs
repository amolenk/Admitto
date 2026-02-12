using Amolenk.Admitto.Shared.Kernel.ErrorHandling;

namespace Amolenk.Admitto.Shared.Kernel.ValueObjects;

public readonly record struct TeamId : IGuidValueObject
{
    public Guid Value { get; }

    private TeamId(Guid value) => Value = value;

    public static TeamId New() => new(Guid.NewGuid());

    public static ValidationResult<TeamId> TryFrom(Guid value)
        => GuidValueObject.TryFrom(value, v => new TeamId(v));

    public static TeamId From(Guid value)
        => GuidValueObject.TryFrom(value, v => new TeamId(v)).GetValueOrThrow();

    public override string ToString() => Value.ToString();
}