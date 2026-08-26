using Amolenk.Admitto.Core.Registrations.Application.UseCases.Registrations.ChangeAttendeeTickets;
using Amolenk.Admitto.Core.Registrations.Domain.Entities;
using Amolenk.Admitto.Core.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Testing.Infrastructure.Assertions;
using Microsoft.EntityFrameworkCore;

namespace Amolenk.Admitto.Core.IntegrationTests.Registrations.Application.UseCases.Registrations.ChangeAttendeeTickets;

[TestClass]
public sealed class ChangeAttendeeTicketsHandlerTests(TestContext testContext) : AspireIntegrationTestBase
{
    private ChangeAttendeeTicketsHandler CreateSut() =>
        new(Environment.RegistrationsDatabase.Context, TimeProvider.System);

    // Given an attendee registered with an early-bird ticket and available workshop capacity
    // When an admin changes the attendee's tickets to the workshop ticket type
    // Then the registration holds the workshop ticket and capacity is released and claimed accordingly
    // Admin changes early-bird → workshop; capacity is updated correctly
    [TestMethod]
    public async ValueTask ChangeAttendeeTickets_HappyPath_TicketsUpdatedAndEventRaised()
    {
        var fixture = ChangeAttendeeTicketsFixture.WithCapacity(earlyBirdMax: 100, earlyBirdUsed: 50,
            workshopMax: 20, workshopUsed: 10);
        await fixture.SetupAsync(Environment);

        var command = new ChangeAttendeeTicketsCommand(
            fixture.EventId.Value,
            fixture.TeamId.Value,
            fixture.RegistrationId.Value,
            [fixture.GetTicketTypeId("workshop").Value],
            ChangeMode.Admin);

        await CreateSut().HandleAsync(command, testContext.CancellationToken);

        await Environment.RegistrationsDatabase.AssertAsync(async dbContext =>
        {
            var registration = await dbContext.Registrations
                .FirstOrDefaultAsync(r => r.Id == fixture.RegistrationId, testContext.CancellationToken);
            registration.ShouldNotBeNull();
            registration.Tickets.Count.ShouldBe(1);
            registration.Tickets[0].Id.ShouldBe(fixture.GetTicketTypeId("workshop"));

            // Capacity: early-bird released (50→49), workshop claimed (10→11)
            var catalog = await dbContext.TicketCatalogs
                .FirstOrDefaultAsync(c => c.Id == fixture.EventId, testContext.CancellationToken);
            catalog.ShouldNotBeNull();
            catalog.GetTicketType(fixture.GetTicketTypeId("early-bird"))!.UsedCapacity.ShouldBe(49);
            catalog.GetTicketType(fixture.GetTicketTypeId("workshop"))!.UsedCapacity.ShouldBe(11);
        });
    }

    // Given a workshop ticket type that is sold out
    // When an admin changes the attendee's tickets to the sold-out workshop
    // Then the change succeeds without enforcing capacity
    // Sold-out workshop does NOT block admin change (enforce: false)
    [TestMethod]
    public async ValueTask ChangeAttendeeTickets_SoldOut_AdminBypassesCapacityEnforcement()
    {
        var fixture = ChangeAttendeeTicketsFixture.WithSoldOutWorkshop();
        await fixture.SetupAsync(Environment);

        var command = new ChangeAttendeeTicketsCommand(
            fixture.EventId.Value,
            fixture.TeamId.Value,
            fixture.RegistrationId.Value,
            [fixture.GetTicketTypeId("workshop").Value],
            ChangeMode.Admin);

        // Should NOT throw
        await CreateSut().HandleAsync(command, testContext.CancellationToken);

        await Environment.RegistrationsDatabase.AssertAsync(async dbContext =>
        {
            var registration = await dbContext.Registrations
                .FirstOrDefaultAsync(r => r.Id == fixture.RegistrationId, testContext.CancellationToken);
            registration.ShouldNotBeNull();
            registration.Tickets.ShouldContain(t => t.Id == fixture.GetTicketTypeId("workshop"));
        });
    }

    // Given a registration that has been cancelled
    // When an admin attempts to change its tickets
    // Then a RegistrationIsCancelled error is thrown
    // Admin attempts to change tickets of a cancelled registration → RegistrationIsCancelled
    [TestMethod]
    public async ValueTask ChangeAttendeeTickets_CancelledRegistration_ThrowsRegistrationIsCancelled()
    {
        var fixture = ChangeAttendeeTicketsFixture.WithCancelledRegistration();
        await fixture.SetupAsync(Environment);

        var command = new ChangeAttendeeTicketsCommand(
            fixture.EventId.Value,
            fixture.TeamId.Value,
            fixture.RegistrationId.Value,
            [fixture.GetTicketTypeId("early-bird").Value],
            ChangeMode.Admin);

        var result = await ErrorResult.CaptureAsync(
            async () => await CreateSut().HandleAsync(command, testContext.CancellationToken));

        result.Error.ShouldMatch(ChangeAttendeeTicketsHandler.Errors.RegistrationIsCancelled);
    }

    // Given a registration that belongs to an archived ticketed event
    // When an admin attempts to change its tickets
    // Then an EventNotActive error is thrown
    // Admin attempts to change tickets for an archived event → EventNotActive
    [TestMethod]
    public async ValueTask ChangeAttendeeTickets_ArchivedEvent_ThrowsEventNotActive()
    {
        var fixture = ChangeAttendeeTicketsFixture.WithArchivedEvent();
        await fixture.SetupAsync(Environment);

        var command = new ChangeAttendeeTicketsCommand(
            fixture.EventId.Value,
            fixture.TeamId.Value,
            fixture.RegistrationId.Value,
            [fixture.GetTicketTypeId("early-bird").Value],
            ChangeMode.Admin);

        var result = await ErrorResult.CaptureAsync(
            async () => await CreateSut().HandleAsync(command, testContext.CancellationToken));

        result.Error.ShouldMatch(TicketCatalog.Errors.EventNotActive);
    }

    // Given an existing registration and a waitlist coupon offering a workshop ticket
    // When the attendee self-serves a ticket change to the offered workshop ticket using the coupon
    // Then the registration's tickets are updated, the coupon is redeemed, and capacity is claimed
    [TestMethod]
    public async ValueTask ChangeAttendeeTickets_WaitlistCouponForExistingRegistration_ChangesTicketsAndRedeemsCoupon()
    {
        var fixture = ChangeAttendeeTicketsFixture.WithWaitlistCoupon();
        await fixture.SetupAsync(Environment);

        var command = new ChangeAttendeeTicketsCommand(
            fixture.EventId.Value,
            fixture.TeamId.Value,
            fixture.RegistrationId.Value,
            [fixture.GetTicketTypeId("workshop").Value],
            ChangeMode.SelfService,
            fixture.WaitlistCouponCode);

        await CreateSut().HandleAsync(command, testContext.CancellationToken);

        await Environment.RegistrationsDatabase.AssertAsync(async dbContext =>
        {
            var registration = await dbContext.Registrations.SingleAsync(testContext.CancellationToken);
            registration.Tickets.ShouldHaveSingleItem().Id.ShouldBe(fixture.GetTicketTypeId("workshop"));

            var coupon = await dbContext.Coupons.SingleAsync(testContext.CancellationToken);
            coupon.RedeemedAt.ShouldNotBeNull();

            var waitlist = await dbContext.Waitlists.SingleAsync(testContext.CancellationToken);
            waitlist.Coupons.ShouldHaveSingleItem().Status.ShouldBe(WaitlistCouponStatus.Redeemed);

            var catalog = await dbContext.TicketCatalogs.SingleAsync(testContext.CancellationToken);
            catalog.GetTicketType(fixture.GetTicketTypeId("early-bird"))!.UsedCapacity.ShouldBe(0);
            catalog.GetTicketType(fixture.GetTicketTypeId("workshop"))!.UsedCapacity.ShouldBe(2);
        });
    }

    // Given a waitlist coupon offering a workshop ticket
    // When the attendee self-serves a ticket change that omits the offered workshop ticket
    // Then a WaitlistCouponTicketMissing error is thrown and the coupon remains unredeemed
    [TestMethod]
    public async ValueTask ChangeAttendeeTickets_WaitlistCouponOfferedTicketMissing_ThrowsAndLeavesCouponUnredeemed()
    {
        var fixture = ChangeAttendeeTicketsFixture.WithWaitlistCoupon();
        await fixture.SetupAsync(Environment);

        var command = new ChangeAttendeeTicketsCommand(
            fixture.EventId.Value,
            fixture.TeamId.Value,
            fixture.RegistrationId.Value,
            [fixture.GetTicketTypeId("early-bird").Value],
            ChangeMode.SelfService,
            fixture.WaitlistCouponCode);

        var result = await ErrorResult.CaptureAsync(
            async () => await CreateSut().HandleAsync(command, testContext.CancellationToken));

        result.Error.ShouldMatch(ChangeAttendeeTicketsHandler.Errors.WaitlistCouponTicketMissing(fixture.GetTicketTypeId("workshop")));

        await Environment.RegistrationsDatabase.AssertAsync(async dbContext =>
        {
            var coupon = await dbContext.Coupons.SingleAsync(testContext.CancellationToken);
            coupon.RedeemedAt.ShouldBeNull();
        });
    }

    // Given a waitlist coupon and ticket types with overlapping time slots
    // When the attendee self-serves a ticket change selecting both overlapping ticket types using the coupon
    // Then an overlapping-time-slots error is thrown and the coupon remains unredeemed
    [TestMethod]
    public async ValueTask ChangeAttendeeTickets_WaitlistCouponFinalSelectionOverlaps_ThrowsAndLeavesCouponUnredeemed()
    {
        var fixture = ChangeAttendeeTicketsFixture.WithWaitlistCoupon(overlappingTickets: true);
        await fixture.SetupAsync(Environment);

        var command = new ChangeAttendeeTicketsCommand(
            fixture.EventId.Value,
            fixture.TeamId.Value,
            fixture.RegistrationId.Value,
            [fixture.GetTicketTypeId("early-bird").Value, fixture.GetTicketTypeId("workshop").Value],
            ChangeMode.SelfService,
            fixture.WaitlistCouponCode);

        var result = await ErrorResult.CaptureAsync(
            async () => await CreateSut().HandleAsync(command, testContext.CancellationToken));

        result.Error.ShouldMatch(TicketCatalog.Errors.OverlappingTimeSlots(["morning"]));

        await Environment.RegistrationsDatabase.AssertAsync(async dbContext =>
        {
            var coupon = await dbContext.Coupons.SingleAsync(testContext.CancellationToken);
            coupon.RedeemedAt.ShouldBeNull();
        });
    }
}
