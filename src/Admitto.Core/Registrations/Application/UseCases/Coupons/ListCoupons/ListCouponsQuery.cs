using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.Coupons.ListCoupons;

internal sealed record ListCouponsQuery(TicketedEventId EventId, TeamId TeamId)
    : Query<ListCouponsResult>;
