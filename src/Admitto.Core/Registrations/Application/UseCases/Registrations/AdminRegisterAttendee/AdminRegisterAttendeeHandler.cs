using Amolenk.Admitto.Core.Registrations.Application.Persistence;
using Amolenk.Admitto.Core.Registrations.Application.UseCases.Registrations.RegisterAttendeeWithCoupon;
using Amolenk.Admitto.Core.Registrations.Contracts;
using Amolenk.Admitto.Core.Registrations.Domain.Entities;
using Amolenk.Admitto.Core.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Amolenk.Admitto.Core.Shared.Application.Persistence;
using Amolenk.Admitto.Core.Shared.Kernel.ErrorHandling;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.Registrations.AdminRegisterAttendee;

internal sealed class AdminRegisterAttendeeHandler(
    IRegistrationsWriteStore writeStore,
    TimeProvider timeProvider)
    : ICommandHandler<AdminRegisterAttendeeCommand, Guid>
{
    public async ValueTask<Guid> HandleAsync(
        AdminRegisterAttendeeCommand command,
        CancellationToken cancellationToken)
    {
        var eventId = TicketedEventId.From(command.EventId);
        var teamId = TeamId.From(command.TeamId);
        var email = EmailAddress.From(command.Email);
        var firstName = FirstName.From(command.FirstName);
        var lastName = LastName.From(command.LastName);

        var ticketedEvent = await writeStore.TicketedEvents
            .GetAsync(e => e.Id == eventId && e.TeamId == teamId, cancellationToken);

        if (!ticketedEvent.IsActive)
            throw new BusinessRuleViolationException(Errors.EventNotActive);

        var additionalDetails = AdditionalDetails.Validate(
            command.AdditionalDetails,
            ticketedEvent.AdditionalDetailSchema);

        var now = timeProvider.GetUtcNow();

        var existingRegistration = await writeStore.Registrations
            .SingleOrDefaultAsync(
                r => r.EventId == eventId && r.TeamId == teamId && r.Email == email,
                cancellationToken);

        if (existingRegistration?.Status == RegistrationStatus.Registered)
            throw new BusinessRuleViolationException(AlreadyExistsError.Create<Registration>());

        var catalog = await writeStore.TicketCatalogs
            .GetAsync(tc => tc.Id == eventId && tc.TeamId == teamId, cancellationToken);

        var ticketTypeIds = command.TicketTypeIds.Select(TicketTypeId.From).ToList();
        var tickets = catalog.Claim(ticketTypeIds, ClaimMode.Admin);

        var waitlists = await writeStore.Waitlists
            .Where(w => w.EventId == eventId && w.TeamId == teamId)
            .ToListAsync(cancellationToken);
        await LeaveWaitlistsAsync(waitlists, catalog, email, ticketTypeIds, now, cancellationToken);

        var waitlistedTickets = RegisterAttendeeWithCouponHandler.DescribeActiveWaitlistEntries(
            waitlists, catalog, email);

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
                additionalDetails,
                now,
                waitlistedTickets);
            await writeStore.Registrations.AddAsync(registration, cancellationToken);
        }
        else
        {
            registration = existingRegistration;
            registration.Reset(firstName, lastName, tickets, additionalDetails, now, waitlistedTickets);
        }

        return registration.Id.Value;
    }

    /// <summary>
    /// For each ticket type the admin registered the attendee for, takes them off that waitlist and withdraws any
    /// offer they hold for it. The admin ticket doesn't redeem the offer: it expires without an expired-offer email,
    /// and an automatic offer's hold goes back to the catalog, so the seat goes to the next person waiting.
    /// </summary>
    private async ValueTask LeaveWaitlistsAsync(
        IReadOnlyList<Waitlist> eventWaitlists,
        TicketCatalog catalog,
        EmailAddress email,
        IReadOnlyList<TicketTypeId> ticketTypeIds,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        foreach (var waitlist in eventWaitlists.Where(w => ticketTypeIds.Contains(w.Id)))
        {
            waitlist.RemoveEntry(email, catalog);

            var issuedCouponIds = waitlist.Coupons
                .Where(c => c.Status == WaitlistCouponStatus.Issued)
                .Select(c => c.Id)
                .ToList();
            if (issuedCouponIds.Count == 0)
                continue;

            var offers = await writeStore.Coupons
                .Where(c => issuedCouponIds.Contains(c.Id) && c.Email == email)
                .ToListAsync(cancellationToken);
            foreach (var offer in offers)
            {
                if (waitlist.WithdrawCoupon(offer.Id, catalog))
                    offer.Expire(now);
            }
        }
    }

    internal static class Errors
    {
        public static readonly Error EventNotActive = new(
            "registration.event_not_active",
            "Cannot register for a cancelled or archived event.",
            Type: ErrorType.Validation);
    }
}
