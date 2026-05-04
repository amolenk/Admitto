using Amolenk.Admitto.Module.Shared.Kernel.ErrorHandling;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Module.Organization.Domain.ValueObjects;

public readonly record struct ApiKeyId : IGuidValueObject
{
    public Guid Value { get; }

    private ApiKeyId(Guid value) => Value = value;

    public static ApiKeyId New() => new(Guid.NewGuid());

    public static ValidationResult<ApiKeyId> TryFrom(Guid value)
        => GuidValueObject.TryFrom(value, v => new ApiKeyId(v));

    public static ApiKeyId From(Guid value) => TryFrom(value).GetValueOrThrow();

    public override string ToString() => Value.ToString();
}
