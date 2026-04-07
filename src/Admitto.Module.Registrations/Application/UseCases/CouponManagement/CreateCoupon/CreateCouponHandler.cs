using Amolenk.Admitto.Module.Registrations.Application.Persistence;
using Amolenk.Admitto.Module.Registrations.Domain.Entities;
using Amolenk.Admitto.Module.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Module.Shared.Application.Messaging;
using Amolenk.Admitto.Module.Shared.Kernel.ErrorHandling;
using Microsoft.EntityFrameworkCore;

namespace Amolenk.Admitto.Module.Registrations.Application.UseCases.CouponManagement.CreateCoupon;

internal sealed class CreateCouponHandler(
    IRegistrationsWriteStore writeStore)
    : ICommandHandler<CreateCouponCommand, CouponId>
{
    public async ValueTask<CouponId> HandleAsync(
        CreateCouponCommand command,
        CancellationToken cancellationToken)
    {
        // Load registration policy and check lifecycle status.
        var policy = await writeStore.EventRegistrationPolicies
            .FirstOrDefaultAsync(p => p.Id == command.EventId, cancellationToken);

        if (policy is null || !policy.IsEventActive)
        {
            throw new BusinessRuleViolationException(Errors.EventNotActive);
        }

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
            DateTimeOffset.UtcNow);

        await writeStore.Coupons.AddAsync(coupon, cancellationToken);

        return coupon.Id;
    }

    internal static class Errors
    {
        public static readonly Error EventNotActive = new(
            "coupon.event_not_active",
            "Coupons cannot be created for cancelled or archived events.",
            Type: ErrorType.Validation);
    }
}
