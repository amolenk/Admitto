using Amolenk.Admitto.Shared.Kernel.ErrorHandling;

namespace Amolenk.Admitto.Shared.Kernel.ValueObjects;

public readonly record struct TeamId : IGuidValueObject
{
    public Guid Value { get; }

    private TeamId(Guid value) => Value = value;

    public static TeamId New() => new(Guid.NewGuid());

    public static ValidationResult<TeamId> TryFrom(Guid value)
        => GuidValueObject.TryFrom(value, v => new TeamId(v), Errors.Empty);

    public static TeamId From(Guid value)
        => GuidValueObject.TryFrom(value, v => new TeamId(v), Errors.Empty).GetValueOrThrow();

    public override string ToString() => Value.ToString();

    private static class Errors
    {
        public static readonly Error Empty =
            new("team_id.empty", "Team ID is required.");
    }
}