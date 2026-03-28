using Amolenk.Admitto.Module.Shared.Kernel.ErrorHandling;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Module.Registrations.Domain.ValueObjects;

public readonly record struct CouponId : IGuidValueObject
{
    public Guid Value { get; }
    
    private CouponId(Guid value) => Value = value;

    public static CouponId New() => new(Guid.NewGuid());

    public static ValidationResult<CouponId> TryFrom(Guid value)
        => GuidValueObject.TryFrom(value, v => new CouponId(v));

    public static CouponId From(Guid value)
        => GuidValueObject.TryFrom(value, v => new CouponId(v)).GetValueOrThrow();

    public override string ToString() => Value.ToString();
}
