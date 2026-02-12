using Amolenk.Admitto.Shared.Kernel.ErrorHandling;
using Amolenk.Admitto.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Organization.Domain.ValueObjects;

/// <summary>
/// Represents the unique identifier for a user in an external user directory.
/// </summary>
public readonly record struct ExternalUserId : IGuidValueObject
{
    public Guid Value { get; }

    private ExternalUserId(Guid value) => Value = value;

    public static ExternalUserId New() => new(Guid.NewGuid());

    public static ValidationResult<ExternalUserId> TryFrom(Guid value)
        => GuidValueObject.TryFrom(value, v => new ExternalUserId(v));

    public static ExternalUserId From(Guid value) => TryFrom(value).GetValueOrThrow();

    public override string ToString() => Value.ToString();
}