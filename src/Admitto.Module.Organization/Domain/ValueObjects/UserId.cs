using Amolenk.Admitto.Module.Shared.Kernel.ErrorHandling;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Module.Organization.Domain.ValueObjects;

/// <summary>
/// Represents the unique identifier for a user in the system.
/// </summary>
public readonly record struct UserId : IGuidValueObject
{
    public Guid Value { get; }

    private UserId(Guid value) => Value = value;

    public static UserId New() => new(Guid.NewGuid());

    public static ValidationResult<UserId> TryFrom(Guid value)
        => GuidValueObject.TryFrom(value, v => new UserId(v));

    public static UserId From(Guid value) => TryFrom(value).GetValueOrThrow();

    public override string ToString() => Value.ToString();
}