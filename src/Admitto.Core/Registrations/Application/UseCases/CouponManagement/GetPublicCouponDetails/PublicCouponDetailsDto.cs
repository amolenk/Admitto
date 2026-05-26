using Amolenk.Admitto.Core.Registrations.Domain.ValueObjects;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.CouponManagement.GetPublicCouponDetails;

public sealed record PublicCouponDetailsDto(
    CouponStatus Status,
    IReadOnlyList<AllowedTicketTypeDto> AllowedTicketTypes,
    DateTimeOffset? ExpiresAt);

public sealed record AllowedTicketTypeDto(Guid Id, string Name);
