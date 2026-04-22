using Amolenk.Admitto.Module.Registrations.Application.Persistence;
using Amolenk.Admitto.Module.Registrations.Domain.Entities;
using Amolenk.Admitto.Module.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Module.Shared.Application.Messaging;
using Amolenk.Admitto.Module.Shared.Kernel.ErrorHandling;
using Microsoft.EntityFrameworkCore;

namespace Amolenk.Admitto.Module.Registrations.Application.UseCases.CouponManagement.CreateCoupon;

// NOTE: EventStatus gating is reintroduced on TicketCatalog in section 7/9 of the
// redesign-ticketed-event-ownership change once the new TicketedEvent aggregate owns
// lifecycle transitions.
internal sealed class CreateCouponHandler(
    IRegistrationsWriteStore writeStore,
    TimeProvider timeProvider)
    : ICommandHandler<CreateCouponCommand, CouponId>
{
    public async ValueTask<CouponId> HandleAsync(
        CreateCouponCommand command,
        CancellationToken cancellationToken)
    {
        var catalog = await writeStore.TicketCatalogs
            .FirstOrDefaultAsync(tc => tc.Id == command.EventId, cancellationToken);

        var availableTicketTypes = catalog?.TicketTypes
            .Select(tt => new TicketTypeInfo(tt.Id, tt.IsCancelled))
            .ToList() ?? [];

        var coupon = Coupon.Create(
            command.EventId,
            command.Email,
            command.AllowedTicketTypeSlugs,
            command.ExpiresAt,
            command.BypassRegistrationWindow,
            availableTicketTypes,
            timeProvider.GetUtcNow());

        await writeStore.Coupons.AddAsync(coupon, cancellationToken);

        return coupon.Id;
    }
}

