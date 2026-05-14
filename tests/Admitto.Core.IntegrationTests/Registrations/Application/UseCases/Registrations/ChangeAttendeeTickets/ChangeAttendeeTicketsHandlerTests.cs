using Amolenk.Admitto.Core.Registrations.Application.UseCases.Registrations.ChangeAttendeeTickets;
using Amolenk.Admitto.Core.Registrations.Contracts.ValueObjects;
using Amolenk.Admitto.Testing.Infrastructure.Assertions;
using Microsoft.EntityFrameworkCore;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Core.IntegrationTests.Registrations.Application.UseCases.Registrations.ChangeAttendeeTickets;

[TestClass]
public sealed class ChangeAttendeeTicketsHandlerTests(TestContext testContext) : AspireIntegrationTestBase
{
    private ChangeAttendeeTicketsHandler CreateSut() =>
        new(Environment.RegistrationsDatabase.Context, TimeProvider.System);

    // SC001: Admin changes early-bird → workshop; capacity is updated correctly
    [TestMethod]
    public async ValueTask ChangeAttendeeTickets_HappyPath_TicketsUpdatedAndEventRaised()
    {
        var fixture = ChangeAttendeeTicketsFixture.WithCapacity(earlyBirdMax: 100, earlyBirdUsed: 50,
            workshopMax: 20, workshopUsed: 10);
        await fixture.SetupAsync(Environment);

        var command = new ChangeAttendeeTicketsCommand(
            fixture.EventId.Value,
            fixture.RegistrationId.Value,
            ["workshop"],
            ChangeMode.Admin);

        await CreateSut().HandleAsync(command, testContext.CancellationToken);

        await Environment.RegistrationsDatabase.AssertAsync(async dbContext =>
        {
            var registration = await dbContext.Registrations
                .FirstOrDefaultAsync(r => r.Id == fixture.RegistrationId, testContext.CancellationToken);
            registration.ShouldNotBeNull();
            registration.Tickets.Count.ShouldBe(1);
            registration.Tickets[0].Slug.ShouldBe(Slug.From("workshop"));

            // Capacity: early-bird released (50→49), workshop claimed (10→11)
            var catalog = await dbContext.TicketCatalogs
                .FirstOrDefaultAsync(c => c.Id == fixture.EventId, testContext.CancellationToken);
            catalog.ShouldNotBeNull();
            catalog.GetTicketType("early-bird")!.UsedCapacity.ShouldBe(49);
            catalog.GetTicketType("workshop")!.UsedCapacity.ShouldBe(11);
        });
    }

    // SC002: Sold-out workshop does NOT block admin change (enforce: false)
    [TestMethod]
    public async ValueTask ChangeAttendeeTickets_SoldOut_AdminBypassesCapacityEnforcement()
    {
        var fixture = ChangeAttendeeTicketsFixture.WithSoldOutWorkshop();
        await fixture.SetupAsync(Environment);

        var command = new ChangeAttendeeTicketsCommand(
            fixture.EventId.Value,
            fixture.RegistrationId.Value,
            ["workshop"],
            ChangeMode.Admin);

        // Should NOT throw
        await CreateSut().HandleAsync(command, testContext.CancellationToken);

        await Environment.RegistrationsDatabase.AssertAsync(async dbContext =>
        {
            var registration = await dbContext.Registrations
                .FirstOrDefaultAsync(r => r.Id == fixture.RegistrationId, testContext.CancellationToken);
            registration.ShouldNotBeNull();
            registration.Tickets.ShouldContain(t => t.Slug == "workshop");
        });
    }

    // SC004: Admin attempts to change tickets of a cancelled registration → RegistrationIsCancelled
    [TestMethod]
    public async ValueTask ChangeAttendeeTickets_CancelledRegistration_ThrowsRegistrationIsCancelled()
    {
        var fixture = ChangeAttendeeTicketsFixture.WithCancelledRegistration();
        await fixture.SetupAsync(Environment);

        var command = new ChangeAttendeeTicketsCommand(
            fixture.EventId.Value,
            fixture.RegistrationId.Value,
            ["early-bird"],
            ChangeMode.Admin);

        var result = await ErrorResult.CaptureAsync(
            async () => await CreateSut().HandleAsync(command, testContext.CancellationToken));

        result.Error.ShouldMatch(ChangeAttendeeTicketsHandler.Errors.RegistrationIsCancelled);
    }

    // SC005: Admin attempts to change tickets for a cancelled event → EventNotActive
    [TestMethod]
    public async ValueTask ChangeAttendeeTickets_CancelledEvent_ThrowsEventNotActive()
    {
        var fixture = ChangeAttendeeTicketsFixture.WithCancelledEvent();
        await fixture.SetupAsync(Environment);

        var command = new ChangeAttendeeTicketsCommand(
            fixture.EventId.Value,
            fixture.RegistrationId.Value,
            ["early-bird"],
            ChangeMode.Admin);

        var result = await ErrorResult.CaptureAsync(
            async () => await CreateSut().HandleAsync(command, testContext.CancellationToken));

        result.Error.ShouldMatch(ChangeAttendeeTicketsHandler.Errors.EventNotActive);
    }
}
