using Amolenk.Admitto.Module.Registrations.Tests.Application.Infrastructure.Hosting;
using Amolenk.Admitto.Module.Registrations.Application.UseCases.Capacity.InitializeTicketCapacity;
using Amolenk.Admitto.Module.Registrations.Application.UseCases.Capacity.UpdateTicketCapacity;
using Amolenk.Admitto.Module.Registrations.Tests.Application.Aspire;
using Microsoft.EntityFrameworkCore;

namespace Amolenk.Admitto.Module.Registrations.Tests.Application.UseCases.Capacity;

[TestClass]
public sealed class CapacitySyncTests(TestContext testContext) : AspireIntegrationTestBase
{
    // 7.4a — TicketTypeAdded creates EventCapacity and TicketCapacity with capacity set
    [TestMethod]
    public async ValueTask SC001_TicketTypeAdded_NewEvent_CreatesEventCapacityWithTicketCapacity()
    {
        var eventId = Guid.NewGuid();
        var command = new InitializeTicketCapacityCommand(eventId, "general-admission", 100);

        var sut = new InitializeTicketCapacityHandler(Environment.Database.Context);
        await sut.HandleAsync(command, testContext.CancellationToken);

        await Environment.Database.AssertAsync(async dbContext =>
        {
            var capacity = await dbContext.EventCapacities.SingleOrDefaultAsync(testContext.CancellationToken);
            capacity.ShouldNotBeNull();
            capacity.Id.Value.ShouldBe(eventId);
            var tc = capacity.TicketCapacities.Single();
            tc.Id.ShouldBe("general-admission");
            tc.MaxCapacity.ShouldBe(100);
            tc.UsedCapacity.ShouldBe(0);
        });
    }

    // 7.4b — TicketTypeAdded with null capacity creates entry with MaxCapacity = null
    [TestMethod]
    public async ValueTask SC002_TicketTypeAdded_NullCapacity_CreatesTicketCapacityWithNullMax()
    {
        var eventId = Guid.NewGuid();
        var command = new InitializeTicketCapacityCommand(eventId, "speaker-pass", null);

        var sut = new InitializeTicketCapacityHandler(Environment.Database.Context);
        await sut.HandleAsync(command, testContext.CancellationToken);

        await Environment.Database.AssertAsync(async dbContext =>
        {
            var capacity = await dbContext.EventCapacities.SingleOrDefaultAsync(testContext.CancellationToken);
            capacity.ShouldNotBeNull();
            var tc = capacity.TicketCapacities.Single();
            tc.MaxCapacity.ShouldBeNull();
        });
    }

    // 7.4c — Second TicketTypeAdded for same event appends to existing EventCapacity
    [TestMethod]
    public async ValueTask SC003_TicketTypeAdded_ExistingEvent_AddsTicketCapacityEntry()
    {
        var eventId = Guid.NewGuid();

        var firstCommand = new InitializeTicketCapacityCommand(eventId, "general-admission", 100);
        var secondCommand = new InitializeTicketCapacityCommand(eventId, "workshop-a", 20);

        var sut = new InitializeTicketCapacityHandler(Environment.Database.Context);
        await sut.HandleAsync(firstCommand, testContext.CancellationToken);

        // Save first so second handler sees the persisted record.
        await Environment.Database.AssertAsync(_ => ValueTask.CompletedTask);

        await sut.HandleAsync(secondCommand, testContext.CancellationToken);

        await Environment.Database.AssertAsync(async dbContext =>
        {
            var capacity = await dbContext.EventCapacities.SingleOrDefaultAsync(testContext.CancellationToken);
            capacity.ShouldNotBeNull();
            capacity.TicketCapacities.Count.ShouldBe(2);
        });
    }

    // 7.4d — TicketTypeCapacityChanged updates MaxCapacity on existing entry
    [TestMethod]
    public async ValueTask SC004_TicketTypeCapacityChanged_UpdatesMaxCapacity()
    {
        var eventId = Guid.NewGuid();

        // Seed initial capacity via initialize handler
        var initCommand = new InitializeTicketCapacityCommand(eventId, "general-admission", 100);
        var initHandler = new InitializeTicketCapacityHandler(Environment.Database.Context);
        await initHandler.HandleAsync(initCommand, testContext.CancellationToken);
        await Environment.Database.AssertAsync(_ => ValueTask.CompletedTask);

        // Now update capacity
        var updateCommand = new UpdateTicketCapacityCommand(eventId, "general-admission", 200);
        var sut = new UpdateTicketCapacityHandler(Environment.Database.Context);
        await sut.HandleAsync(updateCommand, testContext.CancellationToken);

        await Environment.Database.AssertAsync(async dbContext =>
        {
            var capacity = await dbContext.EventCapacities.SingleOrDefaultAsync(testContext.CancellationToken);
            capacity.ShouldNotBeNull();
            capacity.TicketCapacities[0].MaxCapacity.ShouldBe(200);
        });
    }

    // 7.4e — TicketTypeCapacityChanged with null sets MaxCapacity to null
    [TestMethod]
    public async ValueTask SC005_TicketTypeCapacityChanged_NullCapacity_SetsMaxCapacityToNull()
    {
        var eventId = Guid.NewGuid();

        var initCommand = new InitializeTicketCapacityCommand(eventId, "workshop", 50);
        var initHandler = new InitializeTicketCapacityHandler(Environment.Database.Context);
        await initHandler.HandleAsync(initCommand, testContext.CancellationToken);
        await Environment.Database.AssertAsync(_ => ValueTask.CompletedTask);

        var updateCommand = new UpdateTicketCapacityCommand(eventId, "workshop", null);
        var sut = new UpdateTicketCapacityHandler(Environment.Database.Context);
        await sut.HandleAsync(updateCommand, testContext.CancellationToken);

        await Environment.Database.AssertAsync(async dbContext =>
        {
            var capacity = await dbContext.EventCapacities.SingleOrDefaultAsync(testContext.CancellationToken);
            capacity.ShouldNotBeNull();
            capacity.TicketCapacities[0].MaxCapacity.ShouldBeNull();
        });
    }
}
