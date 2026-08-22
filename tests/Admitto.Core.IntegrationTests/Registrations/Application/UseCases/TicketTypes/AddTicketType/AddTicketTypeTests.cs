using Amolenk.Admitto.Core.Registrations.Application.UseCases.TicketTypes.AddTicketType;
using Amolenk.Admitto.Core.Registrations.Domain.Entities;
using Amolenk.Admitto.Core.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Testing.Infrastructure.Assertions;
using Microsoft.EntityFrameworkCore;

namespace Amolenk.Admitto.Core.IntegrationTests.Registrations.Application.UseCases.TicketTypes.AddTicketType;

[TestClass]
public sealed class AddTicketTypeTests(TestContext testContext) : AspireIntegrationTestBase
{
    // Given an active ticketed event with no existing catalog
    // When a ticket type is added to it
    // Then a catalog is created containing the new ticket type with the given properties
    [TestMethod]
    public async ValueTask AddTicketType_ActiveEvent_CreatesCatalogAndTicketType()
    {
        // Arrange
        var fixture = AddTicketTypeFixture.ActiveEvent();
        await fixture.SetupAsync(Environment);

        var command = new AddTicketTypeCommand(
            fixture.EventId.Value,
            fixture.TeamId.Value,
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
            ticketType.MaxReconfirmAttempts.ShouldBeNull();
        });
    }

    // Given an active ticketed event
    // When a ticket type is added with no max capacity specified
    // Then the ticket type is created with a null max capacity
    [TestMethod]
    public async ValueTask AddTicketType_NullMaxCapacity_Succeeds()
    {
        // Arrange
        var fixture = AddTicketTypeFixture.ActiveEvent();
        await fixture.SetupAsync(Environment);

        var command = new AddTicketTypeCommand(
            fixture.EventId.Value,
            fixture.TeamId.Value,
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

    // Given an active event with a catalog that already has a ticket type named "Existing Type"
    // When a ticket type with the same name is added
    // Then a duplicate-name error is returned
    [TestMethod]
    public async ValueTask AddTicketType_DuplicateName_ThrowsDuplicateNameError()
    {
        // Arrange
        var fixture = AddTicketTypeFixture.ActiveEventWithCatalog();
        await fixture.SetupAsync(Environment);

        var command = new AddTicketTypeCommand(
            fixture.EventId.Value,
            fixture.TeamId.Value,
            "Existing Type",
            [],
            50);
        var sut = new AddTicketTypeHandler(Environment.RegistrationsDatabase.Context);

        // Act
        var result = await ErrorResult.CaptureAsync(
            async () => { await sut.HandleAsync(command, testContext.CancellationToken); });

        // Assert
        result.Error.ShouldMatch(TicketCatalog.Errors.DuplicateTicketTypeName(TicketTypeName.From("Existing Type")));
    }

    // Given an active ticketed event
    // When a ticket type is added with a max reconfirm attempts value
    // Then the value is persisted on the created ticket type
    [TestMethod]
    public async ValueTask AddTicketType_WithMaxReconfirmAttempts_PersistsValue()
    {
        var fixture = AddTicketTypeFixture.ActiveEvent();
        await fixture.SetupAsync(Environment);

        var command = new AddTicketTypeCommand(
            fixture.EventId.Value,
            fixture.TeamId.Value,
            "Workshop",
            [],
            50,
            MaxReconfirmAttempts: 3);
        var sut = new AddTicketTypeHandler(Environment.RegistrationsDatabase.Context);

        await sut.HandleAsync(command, testContext.CancellationToken);

        await Environment.RegistrationsDatabase.AssertAsync(async dbContext =>
        {
            var catalog = await dbContext.TicketCatalogs.FirstOrDefaultAsync(c => c.Id == fixture.EventId, testContext.CancellationToken);
            catalog.ShouldNotBeNull();
            catalog.TicketTypes[0].MaxReconfirmAttempts.ShouldBe(3);
        });
    }

    // Given an archived ticketed event
    // When a ticket type is added to it
    // Then an event-not-active error is returned
    [TestMethod]
    public async ValueTask AddTicketType_ArchivedEvent_ThrowsEventNotActive()
    {
        // Arrange
        var fixture = AddTicketTypeFixture.ArchivedEvent();
        await fixture.SetupAsync(Environment);

        var command = new AddTicketTypeCommand(
            fixture.EventId.Value,
            fixture.TeamId.Value,
            "General Admission",
            [],
            100);
        var sut = new AddTicketTypeHandler(Environment.RegistrationsDatabase.Context);

        // Act
        var result = await ErrorResult.CaptureAsync(
            async () => { await sut.HandleAsync(command, testContext.CancellationToken); });

        // Assert
        result.Error.ShouldMatch(TicketCatalog.Errors.EventNotActive);
    }
}
