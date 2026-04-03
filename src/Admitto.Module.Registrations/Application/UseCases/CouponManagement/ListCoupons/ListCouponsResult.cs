using Amolenk.Admitto.Module.Registrations.Domain.ValueObjects;

namespace Amolenk.Admitto.Module.Registrations.Application.UseCases.CouponManagement.ListCoupons;

public sealed record ListCouponsResult(IReadOnlyList<CouponSummaryDto> Coupons);

public sealed record CouponSummaryDto(
    Guid Id,
    string Email,
    CouponStatus Status,
    string[] AllowedTicketTypeSlugs,
    DateTimeOffset ExpiresAt,
    DateTimeOffset CreatedAt);
