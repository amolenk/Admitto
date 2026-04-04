using Amolenk.Admitto.Module.Organization.Contracts;
using Amolenk.Admitto.Module.Registrations.Tests.Application.Infrastructure.Hosting;
using Amolenk.Admitto.Module.Registrations.Application.UseCases.Capacity;
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
        var moduleEvent = new TicketTypeAddedModuleEvent
        {
            TicketedEventId = eventId,
            Slug = "general-admission",
            Name = "General Admission",
            TimeSlots = [],
            Capacity = 100
        };

        var sut = new TicketTypeAddedModuleEventHandler(Environment.Database.Context);
        await sut.HandleAsync(moduleEvent, testContext.CancellationToken);

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
        var moduleEvent = new TicketTypeAddedModuleEvent
        {
            TicketedEventId = eventId,
            Slug = "speaker-pass",
            Name = "Speaker Pass",
            TimeSlots = [],
            Capacity = null
        };

        var sut = new TicketTypeAddedModuleEventHandler(Environment.Database.Context);
        await sut.HandleAsync(moduleEvent, testContext.CancellationToken);

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

        var firstEvent = new TicketTypeAddedModuleEvent
        {
            TicketedEventId = eventId,
            Slug = "general-admission",
            Name = "General Admission",
            TimeSlots = [],
            Capacity = 100
        };
        var secondEvent = new TicketTypeAddedModuleEvent
        {
            TicketedEventId = eventId,
            Slug = "workshop-a",
            Name = "Workshop A",
            TimeSlots = ["morning"],
            Capacity = 20
        };

        var sut = new TicketTypeAddedModuleEventHandler(Environment.Database.Context);
        await sut.HandleAsync(firstEvent, testContext.CancellationToken);

        // Save first so second handler sees the persisted record.
        await Environment.Database.AssertAsync(_ => ValueTask.CompletedTask);

        await sut.HandleAsync(secondEvent, testContext.CancellationToken);

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

        // Seed initial capacity via add handler
        var addEvent = new TicketTypeAddedModuleEvent
        {
            TicketedEventId = eventId,
            Slug = "general-admission",
            Name = "General Admission",
            TimeSlots = [],
            Capacity = 100
        };
        var addHandler = new TicketTypeAddedModuleEventHandler(Environment.Database.Context);
        await addHandler.HandleAsync(addEvent, testContext.CancellationToken);
        await Environment.Database.AssertAsync(_ => ValueTask.CompletedTask);

        // Now update capacity
        var changeEvent = new TicketTypeCapacityChangedModuleEvent
        {
            TicketedEventId = eventId,
            Slug = "general-admission",
            Capacity = 200
        };
        var sut = new TicketTypeCapacityChangedModuleEventHandler(Environment.Database.Context);
        await sut.HandleAsync(changeEvent, testContext.CancellationToken);

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

        var addEvent = new TicketTypeAddedModuleEvent
        {
            TicketedEventId = eventId,
            Slug = "workshop",
            Name = "Workshop",
            TimeSlots = [],
            Capacity = 50
        };
        var addHandler = new TicketTypeAddedModuleEventHandler(Environment.Database.Context);
        await addHandler.HandleAsync(addEvent, testContext.CancellationToken);
        await Environment.Database.AssertAsync(_ => ValueTask.CompletedTask);

        var changeEvent = new TicketTypeCapacityChangedModuleEvent
        {
            TicketedEventId = eventId,
            Slug = "workshop",
            Capacity = null
        };
        var sut = new TicketTypeCapacityChangedModuleEventHandler(Environment.Database.Context);
        await sut.HandleAsync(changeEvent, testContext.CancellationToken);

        await Environment.Database.AssertAsync(async dbContext =>
        {
            var capacity = await dbContext.EventCapacities.SingleOrDefaultAsync(testContext.CancellationToken);
            capacity.ShouldNotBeNull();
            capacity.TicketCapacities[0].MaxCapacity.ShouldBeNull();
        });
    }
}
