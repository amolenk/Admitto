using Vogen;

namespace Amolenk.Admitto.Core.Registrations.Domain.ValueObjects;

[ValueObject<Guid>]
public partial struct CouponCode
{
    public static CouponCode New() => From(Guid.NewGuid());

    private static Validation Validate(Guid value)
        => value != Guid.Empty ? Validation.Ok : Validation.Invalid("Coupon code cannot be empty.");
}
