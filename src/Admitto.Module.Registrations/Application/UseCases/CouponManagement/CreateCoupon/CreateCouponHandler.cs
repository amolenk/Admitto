using Amolenk.Admitto.Module.Organization.Contracts;
using Amolenk.Admitto.Module.Registrations.Application.Persistence;
using Amolenk.Admitto.Module.Registrations.Domain.Entities;
using Amolenk.Admitto.Module.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Module.Shared.Application.Messaging;
using Amolenk.Admitto.Module.Shared.Kernel.ErrorHandling;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Module.Registrations.Application.UseCases.CouponManagement.CreateCoupon;

internal sealed class CreateCouponHandler(
    IOrganizationFacade organizationFacade,
    IRegistrationsWriteStore writeStore)
    : ICommandHandler<CreateCouponCommand, CouponId>
{
    public async ValueTask<CouponId> HandleAsync(
        CreateCouponCommand command,
        CancellationToken cancellationToken)
    {
        // Verify the event is still active (not cancelled or archived).
        var isEventActive = await organizationFacade.IsEventActiveAsync(
            command.EventId.Value, cancellationToken);

        if (!isEventActive)
        {
            throw new BusinessRuleViolationException(Errors.EventNotActive);
        }

        // Fetch ticket types from the Organization module to validate the request.
        var ticketTypeDtos = await organizationFacade.GetTicketTypesAsync(
            command.EventId.Value, cancellationToken);

        var availableTicketTypes = ticketTypeDtos
            .Select(dto => new TicketTypeInfo(TicketTypeId.From(dto.Id), dto.IsCancelled))
            .ToList();

        var coupon = Coupon.Create(
            command.EventId,
            command.Email,
            command.AllowedTicketTypeIds,
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
