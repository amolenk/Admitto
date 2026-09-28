using Amolenk.Admitto.Core.Registrations.Application.UseCases.Waitlists.WithdrawWaitlistEntries.EventHandlers;
using Amolenk.Admitto.Core.Registrations.Domain.DomainEvents;
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
    // When the RegistrationCancelled domain event is handled
    // Then every one of those active entries is removed
    [TestMethod]
    public async ValueTask HandleAsync_EntriesOnMultipleTicketTypes_RemovesEveryActiveEntry()
    {
        var fixture = WithdrawWaitlistEntriesFixture.WithEntriesOnTwoTicketTypes();
        await fixture.SetupAsync(Environment);

        var domainEvent = new RegistrationCancelledDomainEvent(
            fixture.TeamId,
            fixture.EventId,
            RegistrationId.New(),
            fixture.CancelledAttendeeEmail,
            FirstName.From("Alice"),
            LastName.From("Test"),
            CancellationReason.AttendeeRequest);
        var sut = new RegistrationCancelledDomainEventHandler(Environment.RegistrationsDatabase.Context);

        await sut.HandleAsync(domainEvent, testContext.CancellationToken);

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
    // When the RegistrationCancelled domain event is handled for the sandwiched entry's email
    // Then the remaining active entries are renumbered correctly
    [TestMethod]
    public async ValueTask HandleAsync_RemovingEntry_RenumbersRemainingActiveEntries()
    {
        var fixture = WithdrawWaitlistEntriesFixture.WithSurroundingActiveEntries();
        await fixture.SetupAsync(Environment);

        var domainEvent = new RegistrationCancelledDomainEvent(
            fixture.TeamId,
            fixture.EventId,
            RegistrationId.New(),
            fixture.CancelledAttendeeEmail,
            FirstName.From("Alice"),
            LastName.From("Test"),
            CancellationReason.AttendeeRequest);
        var sut = new RegistrationCancelledDomainEventHandler(Environment.RegistrationsDatabase.Context);

        await sut.HandleAsync(domainEvent, testContext.CancellationToken);

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
    // When the RegistrationCancelled domain event is handled
    // Then it completes without throwing and leaves no waitlist behind
    [TestMethod]
    public async ValueTask HandleAsync_NoWaitlistsForEvent_IsNoOp()
    {
        var fixture = WithdrawWaitlistEntriesFixture.WithNoWaitlists();
        await fixture.SetupAsync(Environment);

        var domainEvent = new RegistrationCancelledDomainEvent(
            fixture.TeamId,
            fixture.EventId,
            RegistrationId.New(),
            fixture.CancelledAttendeeEmail,
            FirstName.From("Alice"),
            LastName.From("Test"),
            CancellationReason.AttendeeRequest);
        var sut = new RegistrationCancelledDomainEventHandler(Environment.RegistrationsDatabase.Context);

        await sut.HandleAsync(domainEvent, testContext.CancellationToken);

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
    public async ValueTask HandleAsync_ThenIssueNextCoupon_CancelledAttendeeCannotBePromoted()
    {
        var fixture = WithdrawWaitlistEntriesFixture.WithEntriesOnTwoTicketTypes();
        await fixture.SetupAsync(Environment);

        var domainEvent = new RegistrationCancelledDomainEvent(
            fixture.TeamId,
            fixture.EventId,
            RegistrationId.New(),
            fixture.CancelledAttendeeEmail,
            FirstName.From("Alice"),
            LastName.From("Test"),
            CancellationReason.AttendeeRequest);
        var sut = new RegistrationCancelledDomainEventHandler(Environment.RegistrationsDatabase.Context);

        await sut.HandleAsync(domainEvent, testContext.CancellationToken);

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
            var ticketType = catalog.TicketTypes.Single(tt => tt.Id == fixture.TicketTypeIds[0]);

            var waitlist = await dbContext.Waitlists
                .FirstOrDefaultAsync(w => w.Id == fixture.TicketTypeIds[0], testContext.CancellationToken);
            waitlist.ShouldNotBeNull();

            var coupon = waitlist.IssueNextCoupon(ticketedEvent, ticketType, DateTimeOffset.UtcNow);

            coupon.ShouldBeNull();
        });
    }
}
