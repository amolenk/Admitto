using Amolenk.Admitto.Core.Registrations.Domain.ValueObjects;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.CouponManagement.ListCoupons;

public sealed record ListCouponsResult(IReadOnlyList<CouponSummaryDto> Coupons);

public sealed record CouponSummaryDto(
    Guid Id,
    string Email,
    CouponStatus Status,
    CouponSource Source,
    Guid[] AllowedTicketTypeIds,
    DateTimeOffset ExpiresAt,
    DateTimeOffset CreatedAt);
