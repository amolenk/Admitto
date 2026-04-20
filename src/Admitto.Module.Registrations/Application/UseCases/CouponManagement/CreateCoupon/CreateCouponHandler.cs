using Amolenk.Admitto.Module.Registrations.Application.Persistence;
using Amolenk.Admitto.Module.Registrations.Application.Services;
using Amolenk.Admitto.Module.Registrations.Domain.Entities;
using Amolenk.Admitto.Module.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Module.Shared.Application.Messaging;
using Amolenk.Admitto.Module.Shared.Kernel.ErrorHandling;
using Microsoft.EntityFrameworkCore;

namespace Amolenk.Admitto.Module.Registrations.Application.UseCases.CouponManagement.CreateCoupon;

internal sealed class CreateCouponHandler(
    IRegistrationsWriteStore writeStore,
    TimeProvider timeProvider)
    : ICommandHandler<CreateCouponCommand, CouponId>
{
    public async ValueTask<CouponId> HandleAsync(
        CreateCouponCommand command,
        CancellationToken cancellationToken)
    {
        // Load lifecycle guard and check event is active.
        var guard = await LifecycleGuardStore.LoadOrCreateAsync(writeStore, command.EventId, cancellationToken);
        guard.AssertActiveAndRegisterPolicyMutation();

        // Load ticket catalog to validate the coupon's allowed ticket types.
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
