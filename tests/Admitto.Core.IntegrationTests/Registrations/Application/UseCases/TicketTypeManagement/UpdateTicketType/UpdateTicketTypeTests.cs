using Amolenk.Admitto.Core.Registrations.Application.UseCases.TicketTypeManagement.UpdateTicketType;
using Amolenk.Admitto.Testing.Infrastructure.Assertions;
using Microsoft.EntityFrameworkCore;

namespace Amolenk.Admitto.Core.IntegrationTests.Registrations.Application.UseCases.TicketTypeManagement.UpdateTicketType;

[TestClass]
public sealed class UpdateTicketTypeTests(TestContext testContext) : AspireIntegrationTestBase
{
    // SC-001: Update capacity — succeeds
    [TestMethod]
    public async ValueTask UpdateTicketType_UpdateCapacity_PersistsNewCapacity()
    {
        // Arrange
        var fixture = UpdateTicketTypeFixture.ActiveEvent();
        await fixture.SetupAsync(Environment);

        var command = new UpdateTicketTypeCommand(
            fixture.EventId.Value,
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
            ticketType.MaxReconfirmAttempts.ShouldBeNull();
        });
    }

    // SC-002: Update name — succeeds
    [TestMethod]
    public async ValueTask UpdateTicketType_UpdateName_PersistsNewName()
    {
        // Arrange
        var fixture = UpdateTicketTypeFixture.ActiveEvent();
        await fixture.SetupAsync(Environment);

        var command = new UpdateTicketTypeCommand(
            fixture.EventId.Value,
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

    [TestMethod]
    public async ValueTask UpdateTicketType_WithMaxReconfirmAttempts_PersistsValue()
    {
        var fixture = UpdateTicketTypeFixture.ActiveEvent();
        await fixture.SetupAsync(Environment);

        var command = new UpdateTicketTypeCommand(
            fixture.EventId.Value,
            fixture.TicketTypeId.Value,
            null,
            null,
            MaxReconfirmAttempts: 2,
            UpdateMaxReconfirmAttempts: true);
        var sut = new UpdateTicketTypeHandler(Environment.RegistrationsDatabase.Context);

        await sut.HandleAsync(command, testContext.CancellationToken);

        await Environment.RegistrationsDatabase.AssertAsync(async dbContext =>
        {
            var catalog = await dbContext.TicketCatalogs.FirstOrDefaultAsync(c => c.Id == fixture.EventId, testContext.CancellationToken);
            catalog.ShouldNotBeNull();
            catalog.TicketTypes[0].MaxReconfirmAttempts.ShouldBe(2);
        });
    }

    [TestMethod]
    public async ValueTask UpdateTicketType_ArchivedEvent_ThrowsEventNotActive()
    {
        // Arrange
        var fixture = UpdateTicketTypeFixture.ArchivedEvent();
        await fixture.SetupAsync(Environment);

        var command = new UpdateTicketTypeCommand(
            fixture.EventId.Value,
            fixture.TicketTypeId.Value,
            null,
            200);
        var sut = new UpdateTicketTypeHandler(Environment.RegistrationsDatabase.Context);

        // Act
        var result = await ErrorResult.CaptureAsync(
            async () => { await sut.HandleAsync(command, testContext.CancellationToken); });

        // Assert
        result.Error.Code.ShouldBe("ticket_catalog.event_not_active");
    }
}
