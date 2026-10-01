using Amolenk.Admitto.Core.Registrations.Application.UseCases.Registrations.CancelRegistration;
using Amolenk.Admitto.Core.Registrations.Application.UseCases.Registrations.RegisterAttendee;
using Amolenk.Admitto.Core.Registrations.Application.UseCases.TicketTypes.UpdateTicketType;
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
/// Automatic waitlist offers hold a public seat, and the catalog decides from public capacity how many offers go out.
/// VIP offers and admin tickets come on top of public capacity and never use or free a public seat (ADR-019). Each
/// action runs on a <see cref="DispatchingRegistrationsContext"/>, so the cascade through domain event handlers (e.g.
/// a cancellation releasing a seat that is offered to the queue) happens in the same save, as in production.
/// </summary>
[TestClass]
public sealed class WaitlistHeldCapacityTests(TestContext testContext) : AspireIntegrationTestBase
{
    // Given a sold-out ticket type with two people waiting
    // When the organizer promotes the attendee at position 1 as a VIP
    // Then the offer takes no hold and no other offer goes out
    [TestMethod]
    public async ValueTask PromoteWaitlistEntry_VipWhileSoldOut_TakesNoHoldAndOffersNobodyElse()
    {
        var fixture = WaitlistHeldCapacityFixture.SoldOutWithWaitingEntries(2);
        await fixture.SetupAsync(Environment);

        await PromoteAsync(fixture, position: 1);

        await Environment.RegistrationsDatabase.AssertAsync(async dbContext =>
        {
            (await dbContext.Coupons.SingleAsync(testContext.CancellationToken)).Email
                .ShouldBe(WaitlistHeldCapacityFixture.WaitingEmail(1));

            var ticketType = await GetTicketTypeAsync(dbContext, fixture);
            ticketType.WaitlistHeldCapacity.ShouldBe(0);
            ticketType.AvailableCapacity.ShouldBe(0);
            ticketType.WaitlistMode.ShouldBeTrue();
            await dbContext.ShouldHoldOneSeatPerIssuedAutomaticCouponAsync(fixture.EventId, fixture.TicketTypeId, testContext.CancellationToken);
        });
    }

    // Given a VIP promoted while the ticket type was sold out, with two more people waiting
    // When the VIP registers with their offer, and later cancels
    // Then the redemption is an admin ticket, and the cancellation frees no public seat so nobody is offered
    [TestMethod]
    public async ValueTask CancelRegistration_RedeemedVipCancels_IssuesNoOffer()
    {
        // Arrange — attendee1 promoted as VIP while sold out
        var fixture = WaitlistHeldCapacityFixture.SoldOutWithWaitingEntries(3);
        await fixture.SetupAsync(Environment);
        var vipCouponCode = await PromoteAsync(fixture, position: 1);

        // Act — the VIP redeems their offer
        await RegisterWithCouponAsync(fixture, WaitlistHeldCapacityFixture.WaitingEmail(1), vipCouponCode);

        // Assert — an admin ticket on top of public capacity; the public counters are unchanged
        RegistrationId? vipRegistrationId = null;
        await Environment.RegistrationsDatabase.AssertAsync(async dbContext =>
        {
            var registration = await dbContext.Registrations
                .SingleAsync(r => r.Email == WaitlistHeldCapacityFixture.WaitingEmail(1), testContext.CancellationToken);
            registration.Tickets.ShouldHaveSingleItem().Mode.ShouldBe(ClaimMode.Admin);
            vipRegistrationId = registration.Id;

            var ticketType = await GetTicketTypeAsync(dbContext, fixture);
            ticketType.PublicUsedCapacity.ShouldBe(WaitlistHeldCapacityFixture.PublicCapacity);
            ticketType.AdminUsedCount.ShouldBe(1);
            ticketType.WaitlistHeldCapacity.ShouldBe(0);
            await dbContext.ShouldHoldOneSeatPerIssuedAutomaticCouponAsync(fixture.EventId, fixture.TicketTypeId, testContext.CancellationToken);
        });

        // Act — the VIP cancels
        var published = await CancelAsync(fixture, vipRegistrationId!.Value);

        // Assert — no public seat was freed, so nobody else is offered
        await Environment.RegistrationsDatabase.AssertAsync(async dbContext =>
        {
            (await dbContext.Coupons.CountAsync(testContext.CancellationToken)).ShouldBe(1);

            var ticketType = await GetTicketTypeAsync(dbContext, fixture);
            ticketType.AdminUsedCount.ShouldBe(0);
            ticketType.AvailableCapacity.ShouldBe(0);
            ticketType.WaitlistMode.ShouldBeTrue();
            await dbContext.ShouldHoldOneSeatPerIssuedAutomaticCouponAsync(fixture.EventId, fixture.TicketTypeId, testContext.CancellationToken);
        });
        published.OfType<WaitlistCouponIssuedIntegrationEvent>().ShouldBeEmpty();
    }

    // Given a sold-out ticket type in waitlist mode with people waiting and an admin registration on top
    // When the admin registration is cancelled
    // Then no public seat is freed, so nobody is offered
    [TestMethod]
    public async ValueTask CancelRegistration_AdminRegistrationInWaitlistMode_IssuesNoOffer()
    {
        var fixture = WaitlistHeldCapacityFixture.SoldOutWithWaitingEntriesAndAdminRegistration(2);
        await fixture.SetupAsync(Environment);

        await CancelAsync(fixture, fixture.AdminRegistrationId);

        await Environment.RegistrationsDatabase.AssertAsync(async dbContext =>
        {
            (await dbContext.Coupons.CountAsync(testContext.CancellationToken)).ShouldBe(0);

            var ticketType = await GetTicketTypeAsync(dbContext, fixture);
            ticketType.AdminUsedCount.ShouldBe(0);
            ticketType.PublicUsedCapacity.ShouldBe(WaitlistHeldCapacityFixture.PublicCapacity);
            ticketType.WaitlistMode.ShouldBeTrue();
        });
    }

    // Given a sold-out ticket type with people waiting whose public capacity is lowered by one
    // When two registrations are cancelled
    // Then the first cancellation only makes up the shortfall, and exactly one offer goes out
    [TestMethod]
    public async ValueTask CancelRegistration_AfterPublicCapacityLowered_MakesUpShortfallBeforeOffering()
    {
        // Arrange — 2 of 2 used, capacity lowered to 1
        var fixture = WaitlistHeldCapacityFixture.SoldOutWithWaitingEntries(3);
        await fixture.SetupAsync(Environment);
        await UpdatePublicCapacityAsync(fixture, WaitlistHeldCapacityFixture.PublicCapacity - 1);

        // Act — first cancellation
        await CancelAsync(fixture, fixture.RegistrationIds[0]);

        // Assert — the shortfall is made up, nobody offered
        await Environment.RegistrationsDatabase.AssertAsync(async dbContext =>
        {
            (await dbContext.Coupons.CountAsync(testContext.CancellationToken)).ShouldBe(0);
            (await GetTicketTypeAsync(dbContext, fixture)).AvailableCapacity.ShouldBe(0);
        });

        // Act — second cancellation
        var published = await CancelAsync(fixture, fixture.RegistrationIds[1]);

        // Assert — one offer, to the front of the queue, sent in the same save
        await Environment.RegistrationsDatabase.AssertAsync(async dbContext =>
        {
            (await dbContext.Coupons.SingleAsync(testContext.CancellationToken)).Email
                .ShouldBe(WaitlistHeldCapacityFixture.WaitingEmail(1));

            var waitlist = await dbContext.Waitlists.SingleAsync(testContext.CancellationToken);
            waitlist.GetActivePosition(WaitlistHeldCapacityFixture.WaitingEmail(2)).ShouldBe(1);
            await dbContext.ShouldHoldOneSeatPerIssuedAutomaticCouponAsync(fixture.EventId, fixture.TicketTypeId, testContext.CancellationToken);
        });
        published.OfType<WaitlistCouponIssuedIntegrationEvent>()
            .ShouldHaveSingleItem().RecipientEmail.ShouldBe(WaitlistHeldCapacityFixture.WaitingEmail(1).Value);
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
            await dbContext.ShouldHoldOneSeatPerIssuedAutomaticCouponAsync(fixture.EventId, fixture.TicketTypeId, testContext.CancellationToken);
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
            ticketType.PublicUsedCapacity.ShouldBe(WaitlistHeldCapacityFixture.PublicCapacity);
            ticketType.WaitlistHeldCapacity.ShouldBe(0);
            ticketType.AvailableCapacity.ShouldBe(0);

            var waitlist = await dbContext.Waitlists.SingleAsync(testContext.CancellationToken);
            waitlist.Coupons.ShouldHaveSingleItem().Status.ShouldBe(WaitlistCouponStatus.Redeemed);
            await dbContext.ShouldHoldOneSeatPerIssuedAutomaticCouponAsync(fixture.EventId, fixture.TicketTypeId, testContext.CancellationToken);
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

    private async ValueTask UpdatePublicCapacityAsync(WaitlistHeldCapacityFixture fixture, int publicCapacity)
    {
        await using var dispatch = DispatchingRegistrationsContext.Create(Environment);
        await new UpdateTicketTypeHandler(dispatch.Context).HandleAsync(
            new UpdateTicketTypeCommand(
                fixture.EventId.Value, fixture.TeamId.Value, fixture.TicketTypeId.Value, null, publicCapacity),
            testContext.CancellationToken);
        await dispatch.SaveChangesAsync(testContext.CancellationToken);
    }

    private async ValueTask RegisterWithCouponAsync(
        WaitlistHeldCapacityFixture fixture,
        EmailAddress email,
        Guid couponCode)
    {
        await using var dispatch = DispatchingRegistrationsContext.Create(Environment);
        await new RegisterAttendeeHandler(dispatch.Context, TimeProvider.System).HandleAsync(
            new RegisterAttendeeCommand(
                fixture.EventId.Value,
                fixture.TeamId.Value,
                email.Value,
                "Coupon",
                 "Holder",
                 [fixture.TicketTypeId.Value],
                 [],
                 CouponCode: couponCode),
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
