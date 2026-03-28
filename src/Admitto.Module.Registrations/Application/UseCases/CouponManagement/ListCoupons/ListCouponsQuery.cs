using Amolenk.Admitto.Module.Shared.Application.Messaging;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Module.Registrations.Application.UseCases.CouponManagement.ListCoupons;

internal sealed record ListCouponsQuery(TicketedEventId EventId)
    : Query<ListCouponsResult>;
