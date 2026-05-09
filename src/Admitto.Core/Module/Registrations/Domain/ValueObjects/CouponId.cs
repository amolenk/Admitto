using Vogen;

namespace Amolenk.Admitto.Core.Module.Registrations.Domain.ValueObjects;

[ValueObject<Guid>]
public partial struct CouponId
{
    public static CouponId New() => From(Guid.NewGuid());

    private static Validation Validate(Guid value)
        => value != Guid.Empty ? Validation.Ok : Validation.Invalid("Coupon ID cannot be empty.");
}

