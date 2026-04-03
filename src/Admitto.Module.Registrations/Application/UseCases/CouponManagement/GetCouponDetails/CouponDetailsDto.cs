using Amolenk.Admitto.Module.Registrations.Domain.ValueObjects;

namespace Amolenk.Admitto.Module.Registrations.Application.UseCases.CouponManagement.GetCouponDetails;

public sealed record CouponDetailsDto(
    Guid Id,
    Guid Code,
    string Email,
    CouponStatus Status,
    string[] AllowedTicketTypeSlugs,
    DateTimeOffset ExpiresAt,
    bool BypassRegistrationWindow,
    DateTimeOffset? RedeemedAt,
    DateTimeOffset? RevokedAt,
    DateTimeOffset CreatedAt);
