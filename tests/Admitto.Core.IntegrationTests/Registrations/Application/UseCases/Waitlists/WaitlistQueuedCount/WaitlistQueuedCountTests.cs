using Amolenk.Admitto.Core.Registrations.Application.UseCases.Registrations.CancelRegistration;
using Amolenk.Admitto.Core.Registrations.Application.UseCases.Registrations.ChangeAttendeeTickets;
using Amolenk.Admitto.Core.Registrations.Application.UseCases.Registrations.RegisterAttendee;
using Amolenk.Admitto.Core.Registrations.Application.UseCases.Waitlists.PromoteWaitlistEntry;
using Amolenk.Admitto.Core.Registrations.Application.UseCases.Waitlists.RemoveWaitlistEntry;
using Amolenk.Admitto.Core.Registrations.Contracts.ValueObjects;
using Amolenk.Admitto.Core.Registrations.Domain.Entities;
using Amolenk.Admitto.Core.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Core.Registrations.Infrastructure.Persistence;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace Amolenk.Admitto.Core.IntegrationTests.Registrations.Application.UseCases.Waitlists.WaitlistQueuedCount;

/// <summary>
/// The ticket catalog counts the attendees queued on each ticket type's waitlist, so WaitlistMode is lifted from the
/// catalog's own counts. Each action runs on a <see cref="DispatchingRegistrationsContext"/>, so the cascade through
/// domain event handlers (e.g. a cancellation withdrawing the attendee's waitlist entries) happens in the same save,
/// as in production.
/// </summary>
[TestClass]
public sealed class WaitlistQueuedCountTests(TestContext testContext) : AspireIntegrationTestBase
{
    // Given a sold-out ticket type in waitlist mode with one attendee waiting
    // When another attendee joins the waitlist through self-service registration
    // Then the ticket type counts both attendees as queued
    [TestMethod]
    public async ValueTask RegisterAttendee_JoinsWaitlist_CountsJoinOnCatalog()
    {
        var fixture = WaitlistQueuedCountFixture.SoldOutWithWaitingEntries(1);
        await fixture.SetupAsync(Environment);

        await using (var dispatch = DispatchingRegistrationsContext.Create(Environment))
        {
            await JoinWaitlistAsync(dispatch, fixture, "newcomer@example.com");
            await dispatch.SaveChangesAsync(testContext.CancellationToken);
        }

        await AssertQueuedCountAsync(fixture, expected: 2);
    }

    // Given a sold-out ticket type with two attendees waiting
    // When the organizer removes one of them from the waitlist
    // Then the ticket type counts one attendee as queued
    [TestMethod]
    public async ValueTask RemoveWaitlistEntry_OrganizerRemovesEntry_CountsLeaveOnCatalog()
    {
        var fixture = WaitlistQueuedCountFixture.SoldOutWithWaitingEntries(2);
        await fixture.SetupAsync(Environment);
        var entryId = await GetActiveEntryIdAsync(fixture, position: 1);

        await using (var dispatch = DispatchingRegistrationsContext.Create(Environment))
        {
            await new RemoveWaitlistEntryHandler(dispatch.Context).HandleAsync(
                new RemoveWaitlistEntryCommand(
                    fixture.EventId.Value, fixture.TeamId.Value, fixture.TicketTypeId.Value, entryId.Value),
                testContext.CancellationToken);
            await dispatch.SaveChangesAsync(testContext.CancellationToken);
        }

        await AssertQueuedCountAsync(fixture, expected: 1);
    }

    // Given a sold-out ticket type with two attendees waiting
    // When a registered attendee cancels and the freed seat is offered to the front of the queue
    // Then the ticket type counts one attendee as queued
    [TestMethod]
    public async ValueTask CancelRegistration_FreedSeatOfferedAutomatically_CountsLeaveOnCatalog()
    {
        var fixture = WaitlistQueuedCountFixture.SoldOutWithWaitingEntries(2);
        await fixture.SetupAsync(Environment);

        await CancelAsync(fixture, fixture.RegisteredIds[0]);

        await AssertQueuedCountAsync(fixture, expected: 1);
        await Environment.RegistrationsDatabase.AssertAsync(async dbContext =>
            (await dbContext.Coupons.SingleAsync(testContext.CancellationToken)).Email
                .ShouldBe(WaitlistQueuedCountFixture.WaitingEmail(1)));
    }

    // Given a sold-out ticket type with two attendees waiting
    // When the organizer promotes the second attendee as a VIP
    // Then the ticket type counts one attendee as queued
    [TestMethod]
    public async ValueTask PromoteWaitlistEntry_VipPromotion_CountsLeaveOnCatalog()
    {
        var fixture = WaitlistQueuedCountFixture.SoldOutWithWaitingEntries(2);
        await fixture.SetupAsync(Environment);
        var entryId = await GetActiveEntryIdAsync(fixture, position: 2);

        await using (var dispatch = DispatchingRegistrationsContext.Create(Environment))
        {
            await new PromoteWaitlistEntryHandler(dispatch.Context, TimeProvider.System).HandleAsync(
                new PromoteWaitlistEntryCommand(
                    fixture.EventId.Value, fixture.TeamId.Value, fixture.TicketTypeId.Value, entryId.Value),
                testContext.CancellationToken);
            await dispatch.SaveChangesAsync(testContext.CancellationToken);
        }

        await AssertQueuedCountAsync(fixture, expected: 1);
    }

    // Given a sold-out ticket type with two attendees waiting, the first holding an organiser coupon
    // When the first attendee adds the ticket type to their registration with that coupon
    // Then the redemption removes their waitlist entry and the ticket type counts one attendee as queued
    [TestMethod]
    public async ValueTask ChangeAttendeeTickets_QueuedAttendeeRedeemsCoupon_CountsLeaveOnCatalog()
    {
        var fixture = WaitlistQueuedCountFixture.SoldOutWithWaitingEntriesAndOrganiserCouponForFirst(2);
        await fixture.SetupAsync(Environment);

        await using (var dispatch = DispatchingRegistrationsContext.Create(Environment))
        {
            await new ChangeAttendeeTicketsHandler(dispatch.Context, TimeProvider.System).HandleAsync(
                new ChangeAttendeeTicketsCommand(
                    fixture.EventId.Value,
                    fixture.TeamId.Value,
                    fixture.WaitingIds[0].Value,
                    [fixture.TicketTypeId.Value],
                    ChangeMode.SelfService,
                    fixture.OrganiserCouponCode),
                testContext.CancellationToken);
            await dispatch.SaveChangesAsync(testContext.CancellationToken);
        }

        await AssertQueuedCountAsync(fixture, expected: 1);
        await Environment.RegistrationsDatabase.AssertAsync(async dbContext =>
        {
            var waitlist = await dbContext.Waitlists.AsNoTracking()
                .SingleAsync(w => w.Id == fixture.TicketTypeId, testContext.CancellationToken);
            waitlist.HasActiveEntry(WaitlistQueuedCountFixture.WaitingEmail(1)).ShouldBeFalse();
        });
    }

    // Given a sold-out ticket type with two attendees waiting, each with a waitlisted registration
    // When the first waiting attendee cancels their registration
    // Then they are withdrawn from the waitlist and the ticket type counts one attendee as queued
    [TestMethod]
    public async ValueTask CancelRegistration_WaitlistedAttendeeCancels_CountsLeaveOnCatalog()
    {
        var fixture = WaitlistQueuedCountFixture.SoldOutWithWaitingEntries(2);
        await fixture.SetupAsync(Environment);

        await CancelAsync(fixture, fixture.WaitingIds[0]);

        await AssertQueuedCountAsync(fixture, expected: 1);
    }

    // Given a sold-out ticket type in waitlist mode with nobody waiting, and an attendee part-way through joining the waitlist
    // When a registered attendee cancels, freeing a seat and lifting waitlist mode, before the join is saved
    // Then saving the join fails with a concurrency conflict, so nobody ends up queued with waitlist mode off
    [TestMethod]
    public async ValueTask RegisterAttendee_ConcurrentWaitlistModeLift_JoinFailsWithConcurrencyConflict()
    {
        // Arrange — the joiner has read the catalog (in WaitlistMode) and queued, but not saved yet
        var fixture = WaitlistQueuedCountFixture.SoldOutWithWaitingEntries(0);
        await fixture.SetupAsync(Environment);

        await using var joiner = DispatchingRegistrationsContext.Create(Environment);
        await JoinWaitlistAsync(joiner, fixture, "newcomer@example.com");

        await CancelAsync(fixture, fixture.RegisteredIds[0]);
        await Environment.RegistrationsDatabase.AssertAsync(async dbContext =>
            (await GetTicketTypeAsync(dbContext, fixture)).WaitlistMode.ShouldBeFalse(
                "the cancellation frees a seat with nobody queued, so it lifts WaitlistMode"));

        // Act
        var act = async () => await joiner.SaveChangesAsync(testContext.CancellationToken);

        // Assert
        await Should.ThrowAsync<DbUpdateConcurrencyException>(act);
        await Environment.RegistrationsDatabase.AssertAsync(async dbContext =>
        {
            var waitlist = await dbContext.Waitlists.AsNoTracking()
                .SingleAsync(w => w.Id == fixture.TicketTypeId, testContext.CancellationToken);
            waitlist.ActiveEntryCount.ShouldBe(0);
            await dbContext.ShouldCountEveryActiveEntryAsync(
                fixture.EventId, fixture.TicketTypeId, testContext.CancellationToken);
        });
    }

    // ─── helpers ───────────────────────────────────────────────────────────────

    private async ValueTask JoinWaitlistAsync(
        DispatchingRegistrationsContext dispatch,
        WaitlistQueuedCountFixture fixture,
        string email)
    {
        await new RegisterAttendeeHandler(dispatch.Context, TimeProvider.System).HandleAsync(
            new RegisterAttendeeCommand(
                fixture.EventId.Value,
                fixture.TeamId.Value,
                email,
                "New",
                "Comer",
                RegisterTicketTypeIds: [],
                WaitlistTicketTypeIds: [fixture.TicketTypeId.Value]),
            testContext.CancellationToken);
    }

    private async ValueTask CancelAsync(WaitlistQueuedCountFixture fixture, RegistrationId registrationId)
    {
        await using var dispatch = DispatchingRegistrationsContext.Create(Environment);
        await new CancelRegistrationHandler(dispatch.Context, TimeProvider.System).HandleAsync(
            new CancelRegistrationCommand(
                registrationId.Value, fixture.EventId.Value, fixture.TeamId.Value, CancellationReason.AttendeeRequest),
            testContext.CancellationToken);
        await dispatch.SaveChangesAsync(testContext.CancellationToken);
    }

    private async ValueTask<WaitlistEntryId> GetActiveEntryIdAsync(WaitlistQueuedCountFixture fixture, int position)
    {
        var waitlist = await Environment.RegistrationsDatabase.Context.Waitlists.AsNoTracking()
            .SingleAsync(w => w.Id == fixture.TicketTypeId, testContext.CancellationToken);
        return waitlist.Entries.Single(e => e.Status == WaitlistEntryStatus.Active && e.Position == position).Id;
    }

    private async ValueTask AssertQueuedCountAsync(WaitlistQueuedCountFixture fixture, int expected)
    {
        await Environment.RegistrationsDatabase.AssertAsync(async dbContext =>
        {
            (await GetTicketTypeAsync(dbContext, fixture)).WaitlistQueuedCount.ShouldBe(expected);
            await dbContext.ShouldCountEveryActiveEntryAsync(
                fixture.EventId, fixture.TicketTypeId, testContext.CancellationToken);
        });
    }

    private async ValueTask<TicketType> GetTicketTypeAsync(
        RegistrationsDbContext dbContext,
        WaitlistQueuedCountFixture fixture)
    {
        var catalog = await dbContext.TicketCatalogs.AsNoTracking()
            .SingleAsync(c => c.Id == fixture.EventId, testContext.CancellationToken);
        return catalog.FindTicketType(fixture.TicketTypeId);
    }
}
