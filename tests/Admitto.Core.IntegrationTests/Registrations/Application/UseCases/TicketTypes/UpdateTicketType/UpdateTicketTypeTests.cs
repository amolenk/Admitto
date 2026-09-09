using Amolenk.Admitto.Core.Registrations.Application.UseCases.TicketTypes.UpdateTicketType;
using Amolenk.Admitto.Core.Registrations.Domain.Entities;
using Amolenk.Admitto.Testing.Infrastructure.Assertions;
using Microsoft.EntityFrameworkCore;

namespace Amolenk.Admitto.Core.IntegrationTests.Registrations.Application.UseCases.TicketTypes.UpdateTicketType;

[TestClass]
public sealed class UpdateTicketTypeTests(TestContext testContext) : AspireIntegrationTestBase
{
    // Given an active event with a ticket type
    // When the ticket type's capacity is updated
    // Then the new capacity is persisted and other fields are unchanged
    [TestMethod]
    public async ValueTask UpdateTicketType_UpdateCapacity_PersistsNewCapacity()
    {
        // Arrange
        var fixture = UpdateTicketTypeFixture.ActiveEvent();
        await fixture.SetupAsync(Environment);

        var command = new UpdateTicketTypeCommand(
            fixture.EventId.Value,
            fixture.TeamId.Value,
            fixture.TicketTypeId.Value,
            null,
            200);
        var sut = new UpdateTicketTypeHandler(Environment.RegistrationsDatabase.Context);

        // Act
        await sut.HandleAsync(command, testContext.CancellationToken);

        // Assert
        await Environment.RegistrationsDatabase.AssertAsync(async dbContext =>
        {
            var catalog = await dbContext.TicketCatalogs
                .FirstOrDefaultAsync(tc => tc.Id == fixture.EventId, testContext.CancellationToken);

            catalog.ShouldNotBeNull();
            var ticketType = catalog.TicketTypes.ShouldHaveSingleItem();
            ticketType.MaxCapacity.ShouldBe(200);
            ticketType.Name.Value.ShouldBe("General Admission");
            ticketType.MaxReconfirmationEmails.ShouldBeNull();
        });
    }

    // Given an active event with a ticket type
    // When the ticket type's name is updated
    // Then the new name is persisted
    [TestMethod]
    public async ValueTask UpdateTicketType_UpdateName_PersistsNewName()
    {
        // Arrange
        var fixture = UpdateTicketTypeFixture.ActiveEvent();
        await fixture.SetupAsync(Environment);

        var command = new UpdateTicketTypeCommand(
            fixture.EventId.Value,
            fixture.TeamId.Value,
            fixture.TicketTypeId.Value,
            "VIP Admission",
            null);
        var sut = new UpdateTicketTypeHandler(Environment.RegistrationsDatabase.Context);

        // Act
        await sut.HandleAsync(command, testContext.CancellationToken);

        // Assert
        await Environment.RegistrationsDatabase.AssertAsync(async dbContext =>
        {
            var catalog = await dbContext.TicketCatalogs
                .FirstOrDefaultAsync(tc => tc.Id == fixture.EventId, testContext.CancellationToken);

            catalog.ShouldNotBeNull();
            var ticketType = catalog.TicketTypes.ShouldHaveSingleItem();
            ticketType.Name.Value.ShouldBe("VIP Admission");
        });
    }

    // Given an active event with a ticket type
    // When the ticket type's maximum reconfirmation emails is updated
    // Then the new value is persisted
    [TestMethod]
    public async ValueTask UpdateTicketType_WithMaxReconfirmationEmails_PersistsValue()
    {
        var fixture = UpdateTicketTypeFixture.ActiveEvent();
        await fixture.SetupAsync(Environment);

        var command = new UpdateTicketTypeCommand(
            fixture.EventId.Value,
            fixture.TeamId.Value,
            fixture.TicketTypeId.Value,
            null,
            null,
            MaxReconfirmationEmails: 2,
            UpdateMaxReconfirmationEmails: true);
        var sut = new UpdateTicketTypeHandler(Environment.RegistrationsDatabase.Context);

        await sut.HandleAsync(command, testContext.CancellationToken);

        await Environment.RegistrationsDatabase.AssertAsync(async dbContext =>
        {
            var catalog = await dbContext.TicketCatalogs.FirstOrDefaultAsync(c => c.Id == fixture.EventId, testContext.CancellationToken);
            catalog.ShouldNotBeNull();
            catalog.TicketTypes[0].MaxReconfirmationEmails!.Value.Value.ShouldBe(2);
        });
    }

    // Given an archived event with a ticket type
    // When the ticket type's capacity is updated
    // Then it fails with an event-not-active error
    [TestMethod]
    public async ValueTask UpdateTicketType_ArchivedEvent_ThrowsEventNotActive()
    {
        // Arrange
        var fixture = UpdateTicketTypeFixture.ArchivedEvent();
        await fixture.SetupAsync(Environment);

        var command = new UpdateTicketTypeCommand(
            fixture.EventId.Value,
            fixture.TeamId.Value,
            fixture.TicketTypeId.Value,
            null,
            200);
        var sut = new UpdateTicketTypeHandler(Environment.RegistrationsDatabase.Context);

        // Act
        var result = await ErrorResult.CaptureAsync(
            async () => { await sut.HandleAsync(command, testContext.CancellationToken); });

        // Assert
        result.Error.ShouldMatch(TicketCatalog.Errors.EventNotActive);
    }
}
