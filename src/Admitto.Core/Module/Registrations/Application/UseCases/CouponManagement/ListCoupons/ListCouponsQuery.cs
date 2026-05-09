using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Core.Module.Registrations.Application.UseCases.CouponManagement.ListCoupons;

internal sealed record ListCouponsQuery(TicketedEventId EventId)
    : Query<ListCouponsResult>;
