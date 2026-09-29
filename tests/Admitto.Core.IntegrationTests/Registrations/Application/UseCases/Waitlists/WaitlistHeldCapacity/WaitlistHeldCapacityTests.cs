using Amolenk.Admitto.Core.Registrations.Application.UseCases.Registrations.CancelRegistration;
using Amolenk.Admitto.Core.Registrations.Application.UseCases.Registrations.RegisterAttendeeWithCoupon;
using Amolenk.Admitto.Core.Registrations.Application.UseCases.Waitlists.PromoteWaitlistEntry;
using Amolenk.Admitto.Core.Registrations.Contracts.IntegrationEvents;
using Amolenk.Admitto.Core.Registrations.Contracts.ValueObjects;
using Amolenk.Admitto.Core.Registrations.Domain.Entities;
using Amolenk.Admitto.Core.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Core.Registrations.Infrastructure.Persistence;
using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace Amolenk.Admitto.Core.IntegrationTests.Registrations.Application.UseCases.Waitlists.WaitlistHeldCapacity;

/// <summary>
/// Waitlist offers hold catalog capacity: every outstanding offer holds a seat, and the catalog decides from real
/// capacity how many offers go out. Each action runs on a <see cref="DispatchingRegistrationsContext"/>, so the
/// cascade through domain event handlers (e.g. a cancellation releasing a seat that is offered to the queue) happens
/// in the same save, as in production.
/// </summary>
[TestClass]
public sealed class WaitlistHeldCapacityTests(TestContext testContext) : AspireIntegrationTestBase
{
    // Given a sold-out ticket type with two people waiting
    // When the organizer promotes the attendee at position 1 as a VIP
    // Then the offer holds a seat beyond capacity and no other offer goes out
    [TestMethod]
    public async ValueTask PromoteWaitlistEntry_VipAtPositionOneWhileSoldOut_HoldsSeatWithoutOtherOffers()
    {
        var fixture = WaitlistHeldCapacityFixture.SoldOutWithWaitingEntries(2);
        await fixture.SetupAsync(Environment);

        await PromoteAsync(fixture, position: 1);

        await Environment.RegistrationsDatabase.AssertAsync(async dbContext =>
        {
            (await dbContext.Coupons.SingleAsync(testContext.CancellationToken)).Email
                .ShouldBe(WaitlistHeldCapacityFixture.WaitingEmail(1));

            var ticketType = await GetTicketTypeAsync(dbContext, fixture);
            ticketType.WaitlistHeldCapacity.ShouldBe(1);
            ticketType.AvailableCapacity.ShouldBe(-1);
            ticketType.WaitlistMode.ShouldBeTrue();
            await dbContext.ShouldHoldOneSeatPerIssuedCouponAsync(fixture.EventId, fixture.TicketTypeId, testContext.CancellationToken);
        });
    }

    // Given a sold-out ticket type with a VIP offer outstanding and two more people waiting
    // When a registration is cancelled
    // Then the freed seat covers the VIP offer and nobody else is offered
    [TestMethod]
    public async ValueTask CancelRegistration_VipOfferOutstanding_FreedSeatCoversVipOffer()
    {
        var fixture = WaitlistHeldCapacityFixture.SoldOutWithWaitingEntries(3);
        await fixture.SetupAsync(Environment);
        await PromoteAsync(fixture, position: 1);

        await CancelAsync(fixture, fixture.RegistrationIds[0]);

        await Environment.RegistrationsDatabase.AssertAsync(async dbContext =>
        {
            (await dbContext.Coupons.CountAsync(testContext.CancellationToken)).ShouldBe(1);

            var ticketType = await GetTicketTypeAsync(dbContext, fixture);
            ticketType.AvailableCapacity.ShouldBe(0);
            await dbContext.ShouldHoldOneSeatPerIssuedCouponAsync(fixture.EventId, fixture.TicketTypeId, testContext.CancellationToken);
        });
    }

    // Given a VIP who redeemed their offer while the ticket type was sold out, taking it one over capacity
    // When a registration is cancelled, and then another one
    // Then the first cancellation only pays back the overbooking, and the second offers the seat to the queue
    [TestMethod]
    public async ValueTask CancelRegistration_AfterVipRedeemedOverCapacity_PaysBackOverbookingBeforeOffering()
    {
        // Arrange — attendee1 promoted as VIP and registered: 3 of 2 seats used
        var fixture = WaitlistHeldCapacityFixture.SoldOutWithWaitingEntries(3);
        await fixture.SetupAsync(Environment);
        var vipCouponCode = await PromoteAsync(fixture, position: 1);
        await RegisterWithCouponAsync(fixture, WaitlistHeldCapacityFixture.WaitingEmail(1), vipCouponCode);

        await Environment.RegistrationsDatabase.AssertAsync(async dbContext =>
        {
            var ticketType = await GetTicketTypeAsync(dbContext, fixture);
            ticketType.UsedCapacity.ShouldBe(WaitlistHeldCapacityFixture.MaxCapacity + 1);
            ticketType.WaitlistHeldCapacity.ShouldBe(0);
            await dbContext.ShouldHoldOneSeatPerIssuedCouponAsync(fixture.EventId, fixture.TicketTypeId, testContext.CancellationToken);
        });

        // Act — first cancellation
        await CancelAsync(fixture, fixture.RegistrationIds[0]);

        // Assert — back at capacity, nobody else offered
        await Environment.RegistrationsDatabase.AssertAsync(async dbContext =>
        {
            (await dbContext.Coupons.CountAsync(testContext.CancellationToken)).ShouldBe(1);
            var ticketType = await GetTicketTypeAsync(dbContext, fixture);
            ticketType.UsedCapacity.ShouldBe(WaitlistHeldCapacityFixture.MaxCapacity);
        });

        // Act — second cancellation
        var published = await CancelAsync(fixture, fixture.RegistrationIds[1]);

        // Assert — one offer, to the front of the queue, sent in the same save
        await Environment.RegistrationsDatabase.AssertAsync(async dbContext =>
        {
            var offers = await dbContext.Coupons
                .Where(c => c.Email != WaitlistHeldCapacityFixture.WaitingEmail(1))
                .ToListAsync(testContext.CancellationToken);
            offers.ShouldHaveSingleItem().Email.ShouldBe(WaitlistHeldCapacityFixture.WaitingEmail(2));

            var waitlist = await dbContext.Waitlists.SingleAsync(testContext.CancellationToken);
            waitlist.GetActivePosition(WaitlistHeldCapacityFixture.WaitingEmail(3)).ShouldBe(1);
            await dbContext.ShouldHoldOneSeatPerIssuedCouponAsync(fixture.EventId, fixture.TicketTypeId, testContext.CancellationToken);
        });
        published.OfType<WaitlistCouponIssuedIntegrationEvent>()
            .ShouldHaveSingleItem().RecipientEmail.ShouldBe(WaitlistHeldCapacityFixture.WaitingEmail(2).Value);
    }

    // Given a sold-out ticket type without a reserved buffer, overbooked by an organiser coupon, with people waiting
    // When a registration is cancelled
    // Then the freed seat pays back the overbooking and nobody is offered
    [TestMethod]
    public async ValueTask CancelRegistration_AfterOrganiserCouponOverbooked_IssuesNoOffer()
    {
        var fixture = WaitlistHeldCapacityFixture.SoldOutWithWaitingEntriesAndOrganiserCoupon(2);
        await fixture.SetupAsync(Environment);
        await RegisterWithCouponAsync(
            fixture, WaitlistHeldCapacityFixture.OrganiserGuestEmail, fixture.OrganiserCouponCode);

        await CancelAsync(fixture, fixture.RegistrationIds[0]);

        await Environment.RegistrationsDatabase.AssertAsync(async dbContext =>
        {
            (await dbContext.Coupons.CountAsync(c => c.Source == CouponSource.Waitlist, testContext.CancellationToken))
                .ShouldBe(0);

            var ticketType = await GetTicketTypeAsync(dbContext, fixture);
            ticketType.UsedCapacity.ShouldBe(WaitlistHeldCapacityFixture.MaxCapacity);
            ticketType.WaitlistMode.ShouldBeTrue();
        });
    }

    // Given a sold-out ticket type with people waiting
    // When a registration is cancelled
    // Then the freed seat is offered to the front of the queue, and that offer holds it
    [TestMethod]
    public async ValueTask CancelRegistration_SoldOutWithPeopleWaiting_OffersFreedSeatAndHoldsIt()
    {
        var fixture = WaitlistHeldCapacityFixture.SoldOutWithWaitingEntries(2);
        await fixture.SetupAsync(Environment);

        await CancelAsync(fixture, fixture.RegistrationIds[0]);

        await Environment.RegistrationsDatabase.AssertAsync(async dbContext =>
        {
            (await dbContext.Coupons.SingleAsync(testContext.CancellationToken)).Email
                .ShouldBe(WaitlistHeldCapacityFixture.WaitingEmail(1));

            var ticketType = await GetTicketTypeAsync(dbContext, fixture);
            ticketType.WaitlistHeldCapacity.ShouldBe(1);
            ticketType.IsSoldOut.ShouldBeTrue();
            await dbContext.ShouldHoldOneSeatPerIssuedCouponAsync(fixture.EventId, fixture.TicketTypeId, testContext.CancellationToken);
        });
    }

    // Given an automatic offer sent after a cancellation
    // When the offered attendee registers with it
    // Then the offer's hold turns into their ticket, leaving the ticket type exactly at capacity
    [TestMethod]
    public async ValueTask RegisterWithCoupon_AutomaticOffer_ConvertsHoldIntoTicket()
    {
        var fixture = WaitlistHeldCapacityFixture.SoldOutWithWaitingEntries(2);
        await fixture.SetupAsync(Environment);
        await CancelAsync(fixture, fixture.RegistrationIds[0]);
        var couponCode = (await Environment.RegistrationsDatabase.Context.Coupons.AsNoTracking()
            .SingleAsync(testContext.CancellationToken)).Code.Value;

        await RegisterWithCouponAsync(fixture, WaitlistHeldCapacityFixture.WaitingEmail(1), couponCode);

        await Environment.RegistrationsDatabase.AssertAsync(async dbContext =>
        {
            var ticketType = await GetTicketTypeAsync(dbContext, fixture);
            ticketType.UsedCapacity.ShouldBe(WaitlistHeldCapacityFixture.MaxCapacity);
            ticketType.WaitlistHeldCapacity.ShouldBe(0);
            ticketType.AvailableCapacity.ShouldBe(0);

            var waitlist = await dbContext.Waitlists.SingleAsync(testContext.CancellationToken);
            waitlist.Coupons.ShouldHaveSingleItem().Status.ShouldBe(WaitlistCouponStatus.Redeemed);
            await dbContext.ShouldHoldOneSeatPerIssuedCouponAsync(fixture.EventId, fixture.TicketTypeId, testContext.CancellationToken);
        });
    }

    // ─── helpers ───────────────────────────────────────────────────────────────

    private async ValueTask<Guid> PromoteAsync(WaitlistHeldCapacityFixture fixture, int position)
    {
        var waitlist = await Environment.RegistrationsDatabase.Context.Waitlists.AsNoTracking()
            .SingleAsync(w => w.Id == fixture.TicketTypeId, testContext.CancellationToken);
        var entry = waitlist.Entries.Single(e => e.Status == WaitlistEntryStatus.Active && e.Position == position);

        await using var dispatch = DispatchingRegistrationsContext.Create(Environment);
        var couponId = await new PromoteWaitlistEntryHandler(dispatch.Context, TimeProvider.System).HandleAsync(
            new PromoteWaitlistEntryCommand(
                fixture.EventId.Value, fixture.TeamId.Value, fixture.TicketTypeId.Value, entry.Id.Value),
            testContext.CancellationToken);
        await dispatch.SaveChangesAsync(testContext.CancellationToken);

        var coupon = await Environment.RegistrationsDatabase.Context.Coupons.AsNoTracking()
            .SingleAsync(c => c.Id == CouponId.From(couponId), testContext.CancellationToken);
        return coupon.Code.Value;
    }

    private async ValueTask RegisterWithCouponAsync(
        WaitlistHeldCapacityFixture fixture,
        EmailAddress email,
        Guid couponCode)
    {
        await using var dispatch = DispatchingRegistrationsContext.Create(Environment);
        await new RegisterAttendeeWithCouponHandler(dispatch.Context, TimeProvider.System).HandleAsync(
            new RegisterAttendeeWithCouponCommand(
                fixture.EventId.Value,
                fixture.TeamId.Value,
                email.Value,
                "Coupon",
                "Holder",
                [fixture.TicketTypeId.Value],
                couponCode),
            testContext.CancellationToken);
        await dispatch.SaveChangesAsync(testContext.CancellationToken);
    }

    /// <summary>
    /// Cancels the registration and returns the integration events published in that same save.
    /// </summary>
    private async ValueTask<IReadOnlyList<IIntegrationEvent>> CancelAsync(
        WaitlistHeldCapacityFixture fixture,
        RegistrationId registrationId)
    {
        await using var dispatch = DispatchingRegistrationsContext.Create(Environment);
        await new CancelRegistrationHandler(dispatch.Context, TimeProvider.System).HandleAsync(
            new CancelRegistrationCommand(
                registrationId.Value, fixture.EventId.Value, fixture.TeamId.Value, CancellationReason.VisaLetterDenied),
            testContext.CancellationToken);
        await dispatch.SaveChangesAsync(testContext.CancellationToken);
        return dispatch.PublishedIntegrationEvents.ToList();
    }

    private async ValueTask<TicketType> GetTicketTypeAsync(
        RegistrationsDbContext dbContext,
        WaitlistHeldCapacityFixture fixture)
    {
        var catalog = await dbContext.TicketCatalogs.AsNoTracking()
            .SingleAsync(c => c.Id == fixture.EventId, testContext.CancellationToken);
        return catalog.FindTicketType(fixture.TicketTypeId);
    }
}
