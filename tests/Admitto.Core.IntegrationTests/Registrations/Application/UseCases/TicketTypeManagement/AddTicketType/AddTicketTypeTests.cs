using Amolenk.Admitto.Core.Registrations.Application.UseCases.TicketTypeManagement.AddTicketType;
using Amolenk.Admitto.Core.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Testing.Infrastructure.Assertions;
using Microsoft.EntityFrameworkCore;

namespace Amolenk.Admitto.Core.IntegrationTests.Registrations.Application.UseCases.TicketTypeManagement.AddTicketType;

[TestClass]
public sealed class AddTicketTypeTests(TestContext testContext) : AspireIntegrationTestBase
{
    // SC-001: Add ticket type to active event — succeeds, creates catalog and ticket type
    [TestMethod]
    public async ValueTask AddTicketType_ActiveEvent_CreatesCatalogAndTicketType()
    {
        // Arrange
        var fixture = AddTicketTypeFixture.ActiveEvent();
        await fixture.SetupAsync(Environment);

        var command = new AddTicketTypeCommand(
            fixture.EventId.Value,
            "General Admission",
            ["morning"],
            100);
        var sut = new AddTicketTypeHandler(Environment.RegistrationsDatabase.Context);

        // Act
        var ticketTypeId = await sut.HandleAsync(command, testContext.CancellationToken);

        // Assert
        await Environment.RegistrationsDatabase.AssertAsync(async dbContext =>
        {
            var catalog = await dbContext.TicketCatalogs
                .FirstOrDefaultAsync(tc => tc.Id == fixture.EventId, testContext.CancellationToken);

            catalog.ShouldNotBeNull();
            catalog.TicketTypes.ShouldHaveSingleItem();

            var ticketType = catalog.TicketTypes[0];
            ticketType.Id.Value.ShouldBe(ticketTypeId);
            ticketType.Name.Value.ShouldBe("General Admission");
            ticketType.TimeSlots.ShouldContain(TimeSlot.From("morning"));
            ticketType.MaxCapacity.ShouldBe(100);
            ticketType.IsCancelled.ShouldBeFalse();
        });
    }

    // SC-002: Add ticket type with no max capacity (null) — succeeds
    [TestMethod]
    public async ValueTask AddTicketType_NullMaxCapacity_Succeeds()
    {
        // Arrange
        var fixture = AddTicketTypeFixture.ActiveEvent();
        await fixture.SetupAsync(Environment);

        var command = new AddTicketTypeCommand(
            fixture.EventId.Value,
            "Speaker Pass",
            [],
            null);
        var sut = new AddTicketTypeHandler(Environment.RegistrationsDatabase.Context);

        // Act
        await sut.HandleAsync(command, testContext.CancellationToken);

        // Assert
        await Environment.RegistrationsDatabase.AssertAsync(async dbContext =>
        {
            var catalog = await dbContext.TicketCatalogs
                .FirstOrDefaultAsync(tc => tc.Id == fixture.EventId, testContext.CancellationToken);

            catalog.ShouldNotBeNull();
            var ticketType = catalog.TicketTypes.ShouldHaveSingleItem();
            ticketType.MaxCapacity.ShouldBeNull();
        });
    }

    // SC-003: Reject duplicate name — throws BusinessRuleViolationException
    [TestMethod]
    public async ValueTask AddTicketType_DuplicateName_ThrowsDuplicateNameError()
    {
        // Arrange
        var fixture = AddTicketTypeFixture.ActiveEventWithCatalog();
        await fixture.SetupAsync(Environment);

        var command = new AddTicketTypeCommand(
            fixture.EventId.Value,
            "Existing Type",
            [],
            50);
        var sut = new AddTicketTypeHandler(Environment.RegistrationsDatabase.Context);

        // Act
        var result = await ErrorResult.CaptureAsync(
            async () => { await sut.HandleAsync(command, testContext.CancellationToken); });

        // Assert
        result.Error.Code.ShouldBe("ticket_catalog.duplicate_name");
    }

    // NOTE: SC-004 tests cover event-not-active rejection via TicketCatalog.EventStatus.

    [TestMethod]
    public async ValueTask AddTicketType_CancelledEvent_ThrowsEventNotActive()
    {
        // Arrange
        var fixture = AddTicketTypeFixture.CancelledEvent();
        await fixture.SetupAsync(Environment);

        var command = new AddTicketTypeCommand(
            fixture.EventId.Value,
            "General Admission",
            [],
            100);
        var sut = new AddTicketTypeHandler(Environment.RegistrationsDatabase.Context);

        // Act
        var result = await ErrorResult.CaptureAsync(
            async () => { await sut.HandleAsync(command, testContext.CancellationToken); });

        // Assert
        result.Error.Code.ShouldBe("ticket_catalog.event_not_active");
    }

    [TestMethod]
    public async ValueTask AddTicketType_ArchivedEvent_ThrowsEventNotActive()
    {
        // Arrange
        var fixture = AddTicketTypeFixture.ArchivedEvent();
        await fixture.SetupAsync(Environment);

        var command = new AddTicketTypeCommand(
            fixture.EventId.Value,
            "General Admission",
            [],
            100);
        var sut = new AddTicketTypeHandler(Environment.RegistrationsDatabase.Context);

        // Act
        var result = await ErrorResult.CaptureAsync(
            async () => { await sut.HandleAsync(command, testContext.CancellationToken); });

        // Assert
        result.Error.Code.ShouldBe("ticket_catalog.event_not_active");
    }
}
