using Amolenk.Admitto.Module.Organization.Tests.Application.Infrastructure;
using Amolenk.Admitto.Module.Organization.Application.UseCases.TicketedEvents.AddTicketType;
using Amolenk.Admitto.Module.Organization.Domain.Entities;
using Amolenk.Admitto.Module.Organization.Domain.ValueObjects;
using Amolenk.Admitto.Module.Shared.Kernel.ErrorHandling;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;
using Amolenk.Admitto.Testing.Infrastructure.Assertions;
using Amolenk.Admitto.Testing.Infrastructure.Assertions;
using Should = Shouldly.Should;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace Amolenk.Admitto.Module.Organization.Tests.Application.UseCases.TicketedEvents.AddTicketType;

[TestClass]
public sealed class AddTicketTypeTests(TestContext testContext) : AspireIntegrationTestBase
{
    [TestMethod]
    public async ValueTask SC014_AddTicketType_ActiveEvent_AddsTicketType()
    {
        // Arrange
        // SC-014: Given an active ticketed event with no ticket types, when an organizer adds
        // a ticket type, it is persisted with the correct properties.
        var fixture = AddTicketTypeFixture.ActiveEventWithNoTicketTypes();
        await fixture.SetupAsync(Environment);

        var command = new AddTicketTypeCommand(
            TeamId: fixture.TeamId,
            EventId: fixture.EventId,
            ExpectedVersion: fixture.EventVersion,
            Slug: "vip",
            Name: "VIP Pass",
            TimeSlots: ["morning", "afternoon"],
            Capacity: 50);

        var sut = new AddTicketTypeHandler(Environment.Database.Context);

        // Act
        await sut.HandleAsync(command, testContext.CancellationToken);

        // Assert
        await Environment.Database.AssertAsync(async dbContext =>
        {
            var ticketedEvent = await dbContext.TicketedEvents.FindAsync(
                [TicketedEventId.From(fixture.EventId)],
                testContext.CancellationToken);

            ticketedEvent.ShouldNotBeNull();
            ticketedEvent.TicketTypes.Count.ShouldBe(1);

            var ticketType = ticketedEvent.TicketTypes.Single();
            ticketType.Slug.Value.ShouldBe("vip");
            ticketType.Name.Value.ShouldBe("VIP Pass");
            ticketType.Capacity?.Value.ShouldBe(50);
            ticketType.TimeSlots.Length.ShouldBe(2);
            ticketType.IsCancelled.ShouldBeFalse();
        });
    }

    [TestMethod]
    public async ValueTask SC015_AddTicketType_DuplicateSlug_ThrowsDuplicateTicketTypeSlug()
    {
        // Arrange
        // SC-015: Given an active event that already has a ticket type with slug "general",
        // when an organizer adds another ticket type with the same slug, the request is rejected.
        var fixture = AddTicketTypeFixture.ActiveEventWithExistingTicketType();
        await fixture.SetupAsync(Environment);

        var command = new AddTicketTypeCommand(
            TeamId: fixture.TeamId,
            EventId: fixture.EventId,
            ExpectedVersion: fixture.EventVersion,
            Slug: fixture.ExistingTicketTypeSlug,
            Name: "Duplicate General",
            TimeSlots: ["all-day"],
            Capacity: null);

        var sut = new AddTicketTypeHandler(Environment.Database.Context);

        // Act & Assert
        var exception = await Should.ThrowAsync<BusinessRuleViolationException>(
            async () => await sut.HandleAsync(command, testContext.CancellationToken));

        exception.Error.ShouldMatch(
            TicketedEvent.Errors.DuplicateTicketTypeSlug(Slug.From(fixture.ExistingTicketTypeSlug)));
    }

    [TestMethod]
    public async ValueTask SC016_AddTicketType_CancelledEvent_ThrowsEventCancelled()
    {
        // Arrange
        // SC-016: Given a cancelled ticketed event, when an organizer attempts to add a ticket type,
        // the request is rejected because cancelled events cannot be modified.
        var fixture = AddTicketTypeFixture.CancelledEvent();
        await fixture.SetupAsync(Environment);

        var command = new AddTicketTypeCommand(
            TeamId: fixture.TeamId,
            EventId: fixture.EventId,
            ExpectedVersion: fixture.EventVersion,
            Slug: "standard",
            Name: "Standard Pass",
            TimeSlots: ["all-day"],
            Capacity: null);

        var sut = new AddTicketTypeHandler(Environment.Database.Context);

        // Act & Assert
        var exception = await Should.ThrowAsync<BusinessRuleViolationException>(
            async () => await sut.HandleAsync(command, testContext.CancellationToken));

        exception.Error.ShouldMatch(TicketedEvent.Errors.EventCancelled(TicketedEventId.From(fixture.EventId)));
    }
}
