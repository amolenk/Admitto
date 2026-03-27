namespace Amolenk.Admitto.Module.Registrations.Domain.ValueObjects;

public readonly record struct CouponCode(Guid Value) : IGuidValueObject
{
    public static CouponCode New() => new(Guid.NewGuid());
}