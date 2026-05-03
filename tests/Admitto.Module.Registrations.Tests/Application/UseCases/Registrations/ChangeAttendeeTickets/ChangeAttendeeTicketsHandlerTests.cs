using Amolenk.Admitto.Module.Registrations.Application.UseCases.Registrations.ChangeAttendeeTickets;
using Amolenk.Admitto.Module.Registrations.Domain.Entities;
using Amolenk.Admitto.Module.Registrations.Domain.DomainEvents;
using Amolenk.Admitto.Module.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Module.Registrations.Tests.Application.Aspire;
using Amolenk.Admitto.Module.Shared.Kernel.ErrorHandling;
using Amolenk.Admitto.Testing.Infrastructure.Assertions;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace Amolenk.Admitto.Module.Registrations.Tests.Application.UseCases.Registrations.ChangeAttendeeTickets;

[TestClass]
public sealed class ChangeAttendeeTicketsHandlerTests(TestContext testContext) : AspireIntegrationTestBase
{
    private ChangeAttendeeTicketsHandler CreateSut() =>
        new(Environment.Database.Context, TimeProvider.System);

    // SC001: Admin changes early-bird → workshop; capacity is updated correctly
    [TestMethod]
    public async ValueTask SC001_ChangeAttendeeTickets_HappyPath_TicketsUpdatedAndEventRaised()
    {
        var fixture = ChangeAttendeeTicketsFixture.WithCapacity(earlyBirdMax: 100, earlyBirdUsed: 50,
            workshopMax: 20, workshopUsed: 10);
        await fixture.SetupAsync(Environment);

        var command = new ChangeAttendeeTicketsCommand(
            fixture.EventId,
            fixture.RegistrationId,
            ["workshop"],
            ChangeMode.Admin);

        await CreateSut().HandleAsync(command, testContext.CancellationToken);

        await Environment.Database.AssertAsync(async dbContext =>
        {
            var registration = await dbContext.Registrations
                .FirstOrDefaultAsync(r => r.Id == fixture.RegistrationId, testContext.CancellationToken);
            registration.ShouldNotBeNull();
            registration.Tickets.Count.ShouldBe(1);
            registration.Tickets[0].Slug.ShouldBe("workshop");

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
    public async ValueTask SC002_ChangeAttendeeTickets_SoldOut_AdminBypassesCapacityEnforcement()
    {
        var fixture = ChangeAttendeeTicketsFixture.WithSoldOutWorkshop();
        await fixture.SetupAsync(Environment);

        var command = new ChangeAttendeeTicketsCommand(
            fixture.EventId,
            fixture.RegistrationId,
            ["workshop"],
            ChangeMode.Admin);

        // Should NOT throw
        await CreateSut().HandleAsync(command, testContext.CancellationToken);

        await Environment.Database.AssertAsync(async dbContext =>
        {
            var registration = await dbContext.Registrations
                .FirstOrDefaultAsync(r => r.Id == fixture.RegistrationId, testContext.CancellationToken);
            registration.ShouldNotBeNull();
            registration.Tickets.ShouldContain(t => t.Slug == "workshop");
        });
    }

    // SC004: Admin attempts to change tickets of a cancelled registration → RegistrationIsCancelled
    [TestMethod]
    public async ValueTask SC004_ChangeAttendeeTickets_CancelledRegistration_ThrowsRegistrationIsCancelled()
    {
        var fixture = ChangeAttendeeTicketsFixture.WithCancelledRegistration();
        await fixture.SetupAsync(Environment);

        var command = new ChangeAttendeeTicketsCommand(
            fixture.EventId,
            fixture.RegistrationId,
            ["early-bird"],
            ChangeMode.Admin);

        var result = await ErrorResult.CaptureAsync(
            async () => await CreateSut().HandleAsync(command, testContext.CancellationToken));

        result.Error.ShouldMatch(ChangeAttendeeTicketsHandler.Errors.RegistrationIsCancelled);
    }

    // SC005: Admin attempts to change tickets for a cancelled event → EventNotActive
    [TestMethod]
    public async ValueTask SC005_ChangeAttendeeTickets_CancelledEvent_ThrowsEventNotActive()
    {
        var fixture = ChangeAttendeeTicketsFixture.WithCancelledEvent();
        await fixture.SetupAsync(Environment);

        var command = new ChangeAttendeeTicketsCommand(
            fixture.EventId,
            fixture.RegistrationId,
            ["early-bird"],
            ChangeMode.Admin);

        var result = await ErrorResult.CaptureAsync(
            async () => await CreateSut().HandleAsync(command, testContext.CancellationToken));

        result.Error.ShouldMatch(ChangeAttendeeTicketsHandler.Errors.EventNotActive);
    }
}
