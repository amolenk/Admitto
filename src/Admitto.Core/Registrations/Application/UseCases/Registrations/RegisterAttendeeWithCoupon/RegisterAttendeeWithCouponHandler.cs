using Amolenk.Admitto.Core.Registrations.Application.Persistence;
using Amolenk.Admitto.Core.Registrations.Contracts;
using Amolenk.Admitto.Core.Registrations.Domain.Entities;
using Amolenk.Admitto.Core.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Amolenk.Admitto.Core.Shared.Application.Persistence;
using Amolenk.Admitto.Core.Shared.Kernel.ErrorHandling;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.Registrations.RegisterAttendeeWithCoupon;

internal sealed class RegisterAttendeeWithCouponHandler(
    IRegistrationsWriteStore writeStore,
    TimeProvider timeProvider)
    : ICommandHandler<RegisterAttendeeWithCouponCommand, Guid>
{
    public async ValueTask<Guid> HandleAsync(
        RegisterAttendeeWithCouponCommand command,
        CancellationToken cancellationToken)
    {
        var eventId = TicketedEventId.From(command.EventId);
        var teamId = TeamId.From(command.TeamId);
        var email = EmailAddress.From(command.Email);
        var firstName = FirstName.From(command.FirstName);
        var lastName = LastName.From(command.LastName);
        var ticketTypeIds = command.TicketTypeIds.Select(TicketTypeId.From).ToList();

        var coupon = await writeStore.Coupons.GetAsync(
            c => c.EventId == eventId && c.TeamId == teamId && c.Code == CouponCode.From(command.CouponCode),
            cancellationToken);

        var ticketedEvent = await writeStore.TicketedEvents
            .GetAsync(e => e.Id == eventId && e.TeamId == teamId, cancellationToken);

        if (!ticketedEvent.IsActive)
            throw new BusinessRuleViolationException(Errors.EventNotActive);

        var now = timeProvider.GetUtcNow();
        coupon.Redeem(email, ticketTypeIds, now);

        var additionalDetails = AdditionalDetails.Validate(
            command.AdditionalDetails,
            ticketedEvent.AdditionalDetailSchema);

        if (!coupon.BypassRegistrationWindow)
            ticketedEvent.EnsureRegistrationOpen(now);

        var existingRegistration = await writeStore.Registrations
            .SingleOrDefaultAsync(
                r => r.EventId == eventId && r.TeamId == teamId && r.Email == email,
                cancellationToken);

        if (existingRegistration?.Status == RegistrationStatus.Registered)
            throw new BusinessRuleViolationException(AlreadyExistsError.Create<Registration>());

        var catalog = await writeStore.TicketCatalogs
            .GetAsync(tc => tc.Id == eventId && tc.TeamId == teamId, cancellationToken);

        var tickets = catalog.Claim(ticketTypeIds, enforce: false);

        Registration registration;
        if (existingRegistration is null)
        {
            registration = Registration.Create(
                ticketedEvent.TeamId,
                eventId,
                email,
                firstName,
                lastName,
                tickets,
                additionalDetails);
            await writeStore.Registrations.AddAsync(registration, cancellationToken);
        }
        else
        {
            registration = existingRegistration;
            registration.Reset(firstName, lastName, tickets, additionalDetails);
        }

        if (coupon.Source != CouponSource.Waitlist) return registration.Id.Value;

        var ticketTypeId = TicketTypeId.From(coupon.AllowedTicketTypeIds[0].Value);
        var waitlist = await writeStore.Waitlists
            .GetAsync(w => w.EventId == eventId && w.TeamId == teamId && w.Id == ticketTypeId, cancellationToken);

        waitlist.RedeemCoupon(coupon.Id);

        return registration.Id.Value;
    }

    internal static class Errors
    {
        public static readonly Error EventNotActive = new(
            "registration.event_not_active",
            "Cannot register for a cancelled or archived event.",
            Type: ErrorType.Validation);
    }
}
