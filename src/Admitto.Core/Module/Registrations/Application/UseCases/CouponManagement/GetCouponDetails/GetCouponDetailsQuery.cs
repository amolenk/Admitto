using Amolenk.Admitto.Core.Module.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Core.Module.Registrations.Application.UseCases.CouponManagement.GetCouponDetails;

internal sealed record GetCouponDetailsQuery(TicketedEventId EventId, CouponId CouponId)
    : Query<CouponDetailsDto>;
