using Amolenk.Admitto.Core.Email.Application.Composing;
using Amolenk.Admitto.Core.IntegrationTests.Email.Application.UseCases.Emails.PrepareEmailDelivery.EventHandlers;
using Amolenk.Admitto.Core.Registrations.Application.UseCases.Registrations.ChangeAttendeeTickets;
using Amolenk.Admitto.Core.Registrations.Application.UseCases.Registrations.GetPartnerRegistrationDetails;
using Amolenk.Admitto.Core.Registrations.Application.UseCases.Registrations.UpdatePartnerRegistration;
using Amolenk.Admitto.Core.Registrations.Application.UseCases.Registrations.RegisterAttendeeWithCoupon;
using Amolenk.Admitto.Core.Registrations.Application.UseCases.TicketTypes.UpdateTicketType;
using Amolenk.Admitto.Core.Registrations.Application.UseCases.Waitlists.DisableWaitlist;
using Amolenk.Admitto.Core.Registrations.Application.UseCases.Waitlists.DisableWaitlist.EventHandlers;
using Amolenk.Admitto.Core.Registrations.Application.UseCases.Waitlists.NotifyWaitlist.EventHandlers;
using Amolenk.Admitto.Core.Registrations.Application.UseCases.Waitlists.ProcessWaitlistNotifications;
using Amolenk.Admitto.Core.Registrations.Application.UseCases.Waitlists.PromoteEntireWaitlist;
using Amolenk.Admitto.Core.Registrations.Application.UseCases.Waitlists.PromoteEntireWaitlist.EventHandlers;
using Amolenk.Admitto.Core.Registrations.Contracts;
using Amolenk.Admitto.Core.Registrations.Domain.DomainEvents;
using Amolenk.Admitto.Core.Registrations.Domain.Entities;
using Amolenk.Admitto.Core.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Core.Registrations.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Amolenk.Admitto.Core.IntegrationTests.Registrations.Application.UseCases.TicketTypes.UpdateTicketType;

/// <summary>
/// Covers what happens to the people waiting when an organizer changes a waitlisted ticket type's capacity or
/// disables its waitlist. Domain events are not dispatched in handler-level tests, so each test routes the
/// catalog's events to their real handlers, as <c>DomainEventsInterceptor</c> does in production.
/// </summary>
[TestClass]
public sealed class UpdateTicketTypeWaitlistTests(TestContext testContext) : AspireIntegrationTestBase
{
    // Given a sold-out ticket type with three people waiting
    // When its capacity is raised by two
    // Then the first two people receive a coupon and the third stays waiting at the front of the queue
    [TestMethod]
    public async ValueTask UpdateTicketType_CapacityRaisedWhileWaitlisted_IssuesCouponPerFreedSlot()
    {
        var fixture = UpdateTicketTypeWaitlistFixture.WithWaitingEntries(3);
        await fixture.SetupAsync(Environment);

        await UpdateTicketTypeAsync(fixture, maxCapacity: UpdateTicketTypeWaitlistFixture.MaxCapacity + 2, waitlistEnabled: true);

        await Environment.RegistrationsDatabase.AssertAsync(async dbContext =>
        {
            var coupons = await dbContext.Coupons.ToListAsync(testContext.CancellationToken);
            coupons.Select(c => c.Email).ShouldBe(
                [UpdateTicketTypeWaitlistFixture.WaitingEmail(1), UpdateTicketTypeWaitlistFixture.WaitingEmail(2)],
                ignoreOrder: true);
            coupons.ShouldAllBe(c => c.Source == CouponSource.Waitlist);

            var waitlist = await dbContext.Waitlists.SingleAsync(testContext.CancellationToken);
            waitlist.GetActivePosition(UpdateTicketTypeWaitlistFixture.WaitingEmail(3)).ShouldBe(1);
            waitlist.IssuedCouponCount.ShouldBe(2);

            var ticketType = await GetTicketTypeAsync(dbContext, fixture);
            ticketType.WaitlistEnabled.ShouldBeTrue();
            ticketType.WaitlistMode.ShouldBeTrue();
        });
    }

    // Given a sold-out ticket type with three people waiting
    // When its capacity limit is removed
    // Then everyone waiting receives a coupon and the waitlist is disabled
    [TestMethod]
    public async ValueTask UpdateTicketType_CapacityLimitRemoved_IssuesCouponToEveryEntryAndDisablesWaitlist()
    {
        var fixture = UpdateTicketTypeWaitlistFixture.WithWaitingEntries(3);
        await fixture.SetupAsync(Environment);

        // The Admin UI switches the waitlist off together with the capacity limit.
        await UpdateTicketTypeAsync(fixture, maxCapacity: null, waitlistEnabled: false);

        await Environment.RegistrationsDatabase.AssertAsync(async dbContext =>
        {
            var coupons = await dbContext.Coupons.ToListAsync(testContext.CancellationToken);
            coupons.Select(c => c.Email).ShouldBe(
                Enumerable.Range(1, 3).Select(UpdateTicketTypeWaitlistFixture.WaitingEmail),
                ignoreOrder: true);

            var waitlist = await dbContext.Waitlists.SingleAsync(testContext.CancellationToken);
            waitlist.ActiveEntryCount.ShouldBe(0);
            waitlist.IssuedCouponCount.ShouldBe(3);

            var ticketType = await GetTicketTypeAsync(dbContext, fixture);
            ticketType.MaxCapacity.ShouldBeNull();
            ticketType.WaitlistEnabled.ShouldBeFalse();
            ticketType.WaitlistMode.ShouldBeFalse();
        });
    }

    // Given a sold-out ticket type with two people waiting and an unclaimed offer already sent
    // When the organizer explicitly disables its waitlist
    // Then everyone waiting is removed without an offer, while the outstanding coupon stays valid
    [TestMethod]
    public async ValueTask UpdateTicketType_WaitlistDisabled_RemovesAllEntriesAndKeepsOutstandingCoupon()
    {
        var fixture = UpdateTicketTypeWaitlistFixture.WithWaitingEntriesAndOutstandingCoupon(2);
        await fixture.SetupAsync(Environment);

        await UpdateTicketTypeAsync(fixture, maxCapacity: UpdateTicketTypeWaitlistFixture.MaxCapacity, waitlistEnabled: false);

        await Environment.RegistrationsDatabase.AssertAsync(async dbContext =>
        {
            var coupon = await dbContext.Coupons.SingleAsync(testContext.CancellationToken);
            coupon.Email.ShouldBe(UpdateTicketTypeWaitlistFixture.OfferedEmail);
            coupon.RedeemedAt.ShouldBeNull();

            var waitlist = await dbContext.Waitlists.SingleAsync(testContext.CancellationToken);
            waitlist.Entries.ShouldAllBe(e => e.Status == WaitlistEntryStatus.Removed);
            waitlist.Coupons.ShouldHaveSingleItem().Status.ShouldBe(WaitlistCouponStatus.Issued);

            var ticketType = await GetTicketTypeAsync(dbContext, fixture);
            ticketType.WaitlistEnabled.ShouldBeFalse();
            ticketType.WaitlistMode.ShouldBeFalse();
        });
    }

    // Given a disabled waitlist whose outstanding offer has not been claimed yet
    // When the offered attendee registers with that coupon
    // Then the registration succeeds and the coupon is redeemed
    [TestMethod]
    public async ValueTask UpdateTicketType_WaitlistDisabled_OutstandingCouponStaysRedeemable()
    {
        var fixture = UpdateTicketTypeWaitlistFixture.WithWaitingEntriesAndOutstandingCoupon(1);
        await fixture.SetupAsync(Environment);
        await UpdateTicketTypeAsync(fixture, maxCapacity: UpdateTicketTypeWaitlistFixture.MaxCapacity, waitlistEnabled: false);
        Environment.RegistrationsDatabase.Context.ChangeTracker.Clear();

        var registrationId = await new RegisterAttendeeWithCouponHandler(
                Environment.RegistrationsDatabase.Context, TimeProvider.System)
            .HandleAsync(
                new RegisterAttendeeWithCouponCommand(
                    fixture.EventId.Value,
                    fixture.TeamId.Value,
                    UpdateTicketTypeWaitlistFixture.OfferedEmail.Value,
                    "Olivia",
                    "Offered",
                    [fixture.TicketTypeId.Value],
                    fixture.OutstandingCouponCode),
                testContext.CancellationToken);

        await Environment.RegistrationsDatabase.AssertAsync(async dbContext =>
        {
            var registration = await dbContext.Registrations.SingleAsync(testContext.CancellationToken);
            registration.Id.Value.ShouldBe(registrationId);
            registration.Tickets.ShouldHaveSingleItem().Id.ShouldBe(fixture.TicketTypeId);

            var coupon = await dbContext.Coupons.SingleAsync(testContext.CancellationToken);
            coupon.RedeemedAt.ShouldNotBeNull();

            var waitlist = await dbContext.Waitlists.SingleAsync(testContext.CancellationToken);
            waitlist.Coupons.ShouldHaveSingleItem().Status.ShouldBe(WaitlistCouponStatus.Redeemed);
        });
    }

    // Given a sold-out ticket type with three people waiting
    // When its capacity is raised by one and its waitlist disabled in the same update
    // Then the front of the queue is offered the freed slot first, and the remaining people are removed
    [TestMethod]
    public async ValueTask UpdateTicketType_CapacityRaisedAndWaitlistDisabled_OffersFreedSlotThenRemovesTheRest()
    {
        var fixture = UpdateTicketTypeWaitlistFixture.WithWaitingEntries(3);
        await fixture.SetupAsync(Environment);

        await UpdateTicketTypeAsync(fixture, maxCapacity: UpdateTicketTypeWaitlistFixture.MaxCapacity + 1, waitlistEnabled: false);

        await Environment.RegistrationsDatabase.AssertAsync(async dbContext =>
        {
            var coupon = await dbContext.Coupons.SingleAsync(testContext.CancellationToken);
            coupon.Email.ShouldBe(UpdateTicketTypeWaitlistFixture.WaitingEmail(1));

            var waitlist = await dbContext.Waitlists.SingleAsync(testContext.CancellationToken);
            waitlist.ActiveEntryCount.ShouldBe(0);
            waitlist.IssuedCouponCount.ShouldBe(1);
        });
    }

    // Given a Waitlisted registration whose only waitlisted ticket type is on a waitlist
    // When the organizer explicitly disables that waitlist
    // Then the registration details no longer show the ticket type and the registration stays Waitlisted
    [TestMethod]
    public async ValueTask UpdateTicketType_WaitlistDisabled_RegistrationDetailsNoLongerShowWaitlistedTicketType()
    {
        var fixture = UpdateTicketTypeWaitlistFixture.WithWaitlistedRegistration();
        await fixture.SetupAsync(Environment);

        await UpdateTicketTypeAsync(fixture, maxCapacity: UpdateTicketTypeWaitlistFixture.MaxCapacity, waitlistEnabled: false);
        Environment.RegistrationsDatabase.Context.ChangeTracker.Clear();

        var result = await new GetPartnerRegistrationDetailsHandler(Environment.RegistrationsDatabase.Context)
            .HandleAsync(
                new GetPartnerRegistrationDetailsQuery(
                    fixture.TeamId.Value,
                    fixture.EventId,
                    fixture.WaitlistedRegistrationId.Value),
                testContext.CancellationToken);

        result.ShouldNotBeNull();
        result.Status.ShouldBe(RegistrationStatus.Waitlisted);
        result.TicketTypeIds.ShouldBeEmpty();
        result.WaitlistedTicketTypes.ShouldBeEmpty();
    }

    // Given a registration with a confirmed Workshop ticket that is also waiting for the Conference Pass
    // When the Conference Pass waitlist is disabled and an organizer later adds a Dinner ticket to the registration
    // Then the ticket-changed email lists Workshop and Dinner as confirmed and no longer mentions the Conference Pass
    [TestMethod]
    public async ValueTask UpdateTicketType_WaitlistDisabled_LaterAdminTicketChangeEmailOmitsRemovedWaitlistedTicketType()
    {
        var fixture = UpdateTicketTypeWaitlistFixture.WithMixedRegistration();
        await fixture.SetupAsync(Environment);
        await UpdateTicketTypeAsync(fixture, maxCapacity: UpdateTicketTypeWaitlistFixture.MaxCapacity, waitlistEnabled: false);
        Environment.RegistrationsDatabase.Context.ChangeTracker.Clear();

        await new ChangeAttendeeTicketsHandler(Environment.RegistrationsDatabase.Context, TimeProvider.System)
            .HandleAsync(
                new ChangeAttendeeTicketsCommand(
                    fixture.EventId.Value,
                    fixture.TeamId.Value,
                    fixture.MixedRegistrationId.Value,
                    [fixture.WorkshopTicketTypeId.Value, fixture.DinnerTicketTypeId.Value],
                    ChangeMode.Admin),
                testContext.CancellationToken);

        await AssertTicketChangedEmailOmitsConferencePassAsync(fixture);
    }

    // Given a registration with a confirmed Workshop ticket that is also waiting for the Conference Pass
    // When the Conference Pass waitlist is disabled and the attendee later adds a Dinner ticket themselves
    // Then the ticket-changed email lists Workshop and Dinner as confirmed and no longer mentions the Conference Pass
    [TestMethod]
    public async ValueTask UpdateTicketType_WaitlistDisabled_LaterSelfServiceUpdateEmailOmitsRemovedWaitlistedTicketType()
    {
        var fixture = UpdateTicketTypeWaitlistFixture.WithMixedRegistration();
        await fixture.SetupAsync(Environment);
        await UpdateTicketTypeAsync(fixture, maxCapacity: UpdateTicketTypeWaitlistFixture.MaxCapacity, waitlistEnabled: false);
        Environment.RegistrationsDatabase.Context.ChangeTracker.Clear();

        await new UpdatePartnerRegistrationHandler(Environment.RegistrationsDatabase.Context, TimeProvider.System)
            .HandleAsync(
                new UpdatePartnerRegistrationCommand(
                    fixture.EventId.Value,
                    fixture.TeamId.Value,
                    fixture.MixedRegistrationId.Value,
                    "Alice",
                    "Doe",
                    [fixture.WorkshopTicketTypeId.Value, fixture.DinnerTicketTypeId.Value],
                    []),
                testContext.CancellationToken);

        await AssertTicketChangedEmailOmitsConferencePassAsync(fixture);
    }

    private async ValueTask AssertTicketChangedEmailOmitsConferencePassAsync(UpdateTicketTypeWaitlistFixture fixture)
    {
        var registration = await Environment.RegistrationsDatabase.Context.Registrations
            .FirstAsync(r => r.Id == fixture.MixedRegistrationId, testContext.CancellationToken);
        var domainEvent = registration.GetDomainEvents()
            .OfType<TicketsChangedDomainEvent>()
            .ShouldHaveSingleItem();
        domainEvent.OldWaitlistedTickets.ShouldBeEmpty();
        domainEvent.NewWaitlistedTickets.ShouldBeEmpty();

        var delivery = await TicketConfirmationEmailPipeline.PrepareAsync(
            Environment, fixture.TeamId, fixture.EventId, domainEvent, testContext.CancellationToken);

        delivery.EmailType.ShouldBe(BuiltInEmailTemplateNames.TicketConfirmation);
        delivery.TextBody.ShouldContain("- Workshop");
        delivery.TextBody.ShouldContain("- Dinner");
        delivery.TextBody.ShouldNotContain("Conference Pass");
        delivery.TextBody.ShouldNotContain("You're on the waitlist for:");
    }

    private async ValueTask UpdateTicketTypeAsync(
        UpdateTicketTypeWaitlistFixture fixture,
        int? maxCapacity,
        bool waitlistEnabled)
    {
        var context = Environment.RegistrationsDatabase.Context;
        var cancellationToken = testContext.CancellationToken;

        await new UpdateTicketTypeHandler(context).HandleAsync(
            new UpdateTicketTypeCommand(
                fixture.EventId.Value,
                fixture.TeamId.Value,
                fixture.TicketTypeId.Value,
                Name: null,
                MaxCapacity: maxCapacity,
                WaitlistEnabled: waitlistEnabled),
            cancellationToken);

        var catalog = context.ChangeTracker.Entries<TicketCatalog>().Single().Entity;
        foreach (var domainEvent in catalog.GetDomainEvents().ToList())
        {
            switch (domainEvent)
            {
                case WaitlistCapacityFreedDomainEvent capacityFreed:
                    await new WaitlistCapacityFreedDomainEventHandler(
                            new ProcessWaitlistNotificationsHandler(context, TimeProvider.System))
                        .HandleAsync(capacityFreed, cancellationToken);
                    break;
                case WaitlistDisabledDomainEvent disabled:
                    await new WaitlistDisabledDomainEventHandler(
                            new DisableWaitlistHandler(context, TimeProvider.System))
                        .HandleAsync(disabled, cancellationToken);
                    break;
                case WaitlistCapacityLimitRemovedDomainEvent limitRemoved:
                    await new WaitlistCapacityLimitRemovedDomainEventHandler(
                            new PromoteEntireWaitlistHandler(context, TimeProvider.System))
                        .HandleAsync(limitRemoved, cancellationToken);
                    break;
            }
        }

        catalog.ClearDomainEvents();
        await context.SaveChangesAsync(cancellationToken);
    }

    private async ValueTask<TicketType> GetTicketTypeAsync(
        RegistrationsDbContext dbContext,
        UpdateTicketTypeWaitlistFixture fixture)
    {
        var catalog = await dbContext.TicketCatalogs.SingleAsync(
            c => c.Id == fixture.EventId, testContext.CancellationToken);
        return catalog.TicketTypes.Single(t => t.Id == fixture.TicketTypeId);
    }
}
