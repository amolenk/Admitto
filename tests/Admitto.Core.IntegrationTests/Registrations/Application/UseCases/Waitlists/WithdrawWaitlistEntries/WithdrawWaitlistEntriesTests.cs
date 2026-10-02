using Amolenk.Admitto.Core.Registrations.Application.UseCases.Waitlists.WithdrawWaitlistEntries;
using Amolenk.Admitto.Core.Registrations.Domain.Entities;
using Amolenk.Admitto.Core.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Core.Registrations.Contracts.ValueObjects;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace Amolenk.Admitto.Core.IntegrationTests.Registrations.Application.UseCases.Waitlists.WithdrawWaitlistEntries;

[TestClass]
public sealed class WithdrawWaitlistEntriesTests(TestContext testContext) : AspireIntegrationTestBase
{
    // Given a cancelled attendee with active waitlist entries on two different ticket types for the event
    // When waitlist entries are withdrawn for that email
    // Then every one of those active entries is removed
    [TestMethod]
    public async ValueTask WithdrawWaitlistEntries_EntriesOnMultipleTicketTypes_RemovesEveryActiveEntry()
    {
        var fixture = WithdrawWaitlistEntriesFixture.WithEntriesOnTwoTicketTypes();
        await fixture.SetupAsync(Environment);

        var sut = new WithdrawWaitlistEntriesHandler(Environment.RegistrationsDatabase.Context);

        await sut.HandleAsync(
            new WithdrawWaitlistEntriesCommand(
                fixture.EventId.Value, fixture.TeamId.Value, fixture.CancelledAttendeeEmail.Value),
            testContext.CancellationToken);

        await Environment.RegistrationsDatabase.AssertAsync(async dbContext =>
        {
            var waitlists = await dbContext.Waitlists
                .Where(w => fixture.TicketTypeIds.Contains(w.Id))
                .ToListAsync(testContext.CancellationToken);

            waitlists.Count.ShouldBe(2);
            waitlists.ShouldAllBe(w =>
                w.Entries.Single(e => e.Email == fixture.CancelledAttendeeEmail).Status
                    == WaitlistEntryStatus.Removed);
        });
    }

    // Given a waitlist entry sandwiched between two other active entries
    // When waitlist entries are withdrawn for the sandwiched entry's email
    // Then the remaining active entries are renumbered correctly
    [TestMethod]
    public async ValueTask WithdrawWaitlistEntries_RemovingEntry_RenumbersRemainingActiveEntries()
    {
        var fixture = WithdrawWaitlistEntriesFixture.WithSurroundingActiveEntries();
        await fixture.SetupAsync(Environment);

        var sut = new WithdrawWaitlistEntriesHandler(Environment.RegistrationsDatabase.Context);

        await sut.HandleAsync(
            new WithdrawWaitlistEntriesCommand(
                fixture.EventId.Value, fixture.TeamId.Value, fixture.CancelledAttendeeEmail.Value),
            testContext.CancellationToken);

        await Environment.RegistrationsDatabase.AssertAsync(async dbContext =>
        {
            var waitlist = await dbContext.Waitlists
                .FirstOrDefaultAsync(w => w.Id == fixture.TicketTypeIds[0], testContext.CancellationToken);

            waitlist.ShouldNotBeNull();

            waitlist.Entries.Single(e => e.Email == fixture.CancelledAttendeeEmail)
                .Status.ShouldBe(WaitlistEntryStatus.Removed);

            var before = waitlist.Entries.Single(e => e.Email == fixture.OtherAttendeeBeforeEmail);
            before.Status.ShouldBe(WaitlistEntryStatus.Active);
            before.Position.ShouldBe(1);

            var after = waitlist.Entries.Single(e => e.Email == fixture.OtherAttendeeAfterEmail);
            after.Status.ShouldBe(WaitlistEntryStatus.Active);
            after.Position.ShouldBe(2);
        });
    }

    // Given an event with no waitlists at all
    // When waitlist entries are withdrawn for an attendee's email
    // Then it completes without throwing and leaves no waitlist behind
    [TestMethod]
    public async ValueTask WithdrawWaitlistEntries_NoWaitlistsForEvent_IsNoOp()
    {
        var fixture = WithdrawWaitlistEntriesFixture.WithNoWaitlists();
        await fixture.SetupAsync(Environment);

        var sut = new WithdrawWaitlistEntriesHandler(Environment.RegistrationsDatabase.Context);

        await sut.HandleAsync(
            new WithdrawWaitlistEntriesCommand(
                fixture.EventId.Value, fixture.TeamId.Value, fixture.CancelledAttendeeEmail.Value),
            testContext.CancellationToken);

        await Environment.RegistrationsDatabase.AssertAsync(async dbContext =>
        {
            var waitlistCount = await dbContext.Waitlists.CountAsync(testContext.CancellationToken);
            waitlistCount.ShouldBe(0);
        });
    }

    // Given a cancelled attendee whose only waitlist entry has just been withdrawn
    // When the next coupon is issued from that waitlist
    // Then no coupon is issued because the cancelled attendee's entry no longer exists
    [TestMethod]
    public async ValueTask WithdrawWaitlistEntries_ThenIssueNextCoupon_CancelledAttendeeCannotBePromoted()
    {
        var fixture = WithdrawWaitlistEntriesFixture.WithEntriesOnTwoTicketTypes();
        await fixture.SetupAsync(Environment);

        var sut = new WithdrawWaitlistEntriesHandler(Environment.RegistrationsDatabase.Context);

        await sut.HandleAsync(
            new WithdrawWaitlistEntriesCommand(
                fixture.EventId.Value, fixture.TeamId.Value, fixture.CancelledAttendeeEmail.Value),
            testContext.CancellationToken);

        await Environment.RegistrationsDatabase.AssertAsync(async dbContext =>
        {
            var ticketedEvent = TicketedEvent.Create(
                CreationRequestId.From(Guid.NewGuid()),
                fixture.EventId,
                fixture.TeamId,
                EventName.From("DevConf"),
                AbsoluteUrl.From("https://example.com"),
                AbsoluteUrl.From("https://tickets.example.com"),
                DateTimeOffset.UtcNow.AddDays(10),
                DateTimeOffset.UtcNow.AddDays(11),
                TimeZoneId.From("UTC"));

            var catalog = TicketCatalog.Create(fixture.EventId, fixture.TeamId);
            catalog.AddTicketType(fixture.TicketTypeIds[0], TicketTypeName.From("General Admission"), [], 1);

            var waitlist = await dbContext.Waitlists
                .FirstOrDefaultAsync(w => w.Id == fixture.TicketTypeIds[0], testContext.CancellationToken);
            waitlist.ShouldNotBeNull();

            var coupon = waitlist.IssueNextCoupon(ticketedEvent, catalog, DateTimeOffset.UtcNow);

            coupon.ShouldBeNull();
        });
    }

    // Given a cancelled attendee holding an outstanding automatic waitlist offer, with another attendee queued behind
    // When waitlist entries are withdrawn for that email
    // Then the offer is withdrawn — its coupon expired and its held seat released — without telling its recipient
    [TestMethod]
    public async ValueTask WithdrawWaitlistEntries_OutstandingAutomaticOffer_WithdrawsOfferAndReleasesHold()
    {
        var fixture = WithdrawWaitlistEntriesFixture.WithOutstandingOffer();
        await fixture.SetupAsync(Environment);

        var sut = new WithdrawWaitlistEntriesHandler(Environment.RegistrationsDatabase.Context);

        await sut.HandleAsync(
            new WithdrawWaitlistEntriesCommand(
                fixture.EventId.Value, fixture.TeamId.Value, fixture.CancelledAttendeeEmail.Value),
            testContext.CancellationToken);

        await Environment.RegistrationsDatabase.AssertAsync(async dbContext =>
        {
            var waitlist = await dbContext.Waitlists
                .SingleAsync(w => w.Id == fixture.TicketTypeIds[0], testContext.CancellationToken);

            waitlist.Entries.Single(e => e.Email == fixture.CancelledAttendeeEmail)
                .Status.ShouldBe(WaitlistEntryStatus.Removed);

            // The attendee still queued behind them keeps their place; promoting them to the next offer
            // is the job of the released-capacity domain event handler, not this one.
            var nextEntry = waitlist.Entries.Single(e => e.Email == fixture.NextQueuedEmail);
            nextEntry.Status.ShouldBe(WaitlistEntryStatus.Active);

            var catalog = await dbContext.TicketCatalogs
                .SingleAsync(c => c.Id == fixture.EventId, testContext.CancellationToken);
            catalog.GetTicketType(fixture.TicketTypeIds[0])!.WaitlistHeldCapacity.ShouldBe(0);

            var coupon = await dbContext.Coupons.SingleAsync(
                c => c.Id == fixture.OfferedCouponId, testContext.CancellationToken);
            coupon.GetStatus(DateTimeOffset.UtcNow).ShouldBe(CouponStatus.Expired);
        });
    }
}
