using Amolenk.Admitto.Module.Registrations.Application.UseCases.TicketTypeManagement.UpdateTicketType;
using Amolenk.Admitto.Module.Registrations.Domain.Entities;
using Amolenk.Admitto.Module.Registrations.Tests.Application.Aspire;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;
using Amolenk.Admitto.Testing.Infrastructure.Assertions;
using Microsoft.EntityFrameworkCore;

namespace Amolenk.Admitto.Module.Registrations.Tests.Application.UseCases.TicketTypeManagement.UpdateTicketType;

[TestClass]
public sealed class UpdateTicketTypeTests(TestContext testContext) : AspireIntegrationTestBase
{
    // SC-001: Update capacity — succeeds
    [TestMethod]
    public async ValueTask SC001_UpdateTicketType_UpdateCapacity_PersistsNewCapacity()
    {
        // Arrange
        var fixture = UpdateTicketTypeFixture.ActiveEvent();
        await fixture.SetupAsync(Environment);

        var command = new UpdateTicketTypeCommand(
            fixture.EventId,
            Slug.From(fixture.TicketTypeSlug),
            null,
            200);
        var sut = new UpdateTicketTypeHandler(Environment.Database.Context);

        // Act
        await sut.HandleAsync(command, testContext.CancellationToken);

        // Assert
        await Environment.Database.AssertAsync(async dbContext =>
        {
            var catalog = await dbContext.TicketCatalogs
                .FirstOrDefaultAsync(tc => tc.Id == fixture.EventId, testContext.CancellationToken);

            catalog.ShouldNotBeNull();
            var ticketType = catalog.TicketTypes.ShouldHaveSingleItem();
            ticketType.MaxCapacity.ShouldBe(200);
            ticketType.Name.Value.ShouldBe("General Admission");
        });
    }

    // SC-002: Update name — succeeds
    [TestMethod]
    public async ValueTask SC002_UpdateTicketType_UpdateName_PersistsNewName()
    {
        // Arrange
        var fixture = UpdateTicketTypeFixture.ActiveEvent();
        await fixture.SetupAsync(Environment);

        var command = new UpdateTicketTypeCommand(
            fixture.EventId,
            Slug.From(fixture.TicketTypeSlug),
            DisplayName.From("VIP Admission"),
            null);
        var sut = new UpdateTicketTypeHandler(Environment.Database.Context);

        // Act
        await sut.HandleAsync(command, testContext.CancellationToken);

        // Assert
        await Environment.Database.AssertAsync(async dbContext =>
        {
            var catalog = await dbContext.TicketCatalogs
                .FirstOrDefaultAsync(tc => tc.Id == fixture.EventId, testContext.CancellationToken);

            catalog.ShouldNotBeNull();
            var ticketType = catalog.TicketTypes.ShouldHaveSingleItem();
            ticketType.Name.Value.ShouldBe("VIP Admission");
        });
    }

    // SC-003: Reject on cancelled event — throws BusinessRuleViolationException
    [TestMethod]
    public async ValueTask SC003_UpdateTicketType_CancelledEvent_ThrowsEventNotActiveError()
    {
        // Arrange
        var fixture = UpdateTicketTypeFixture.CancelledEvent();
        await fixture.SetupAsync(Environment);

        var command = new UpdateTicketTypeCommand(
            fixture.EventId,
            Slug.From(fixture.TicketTypeSlug),
            DisplayName.From("Updated Name"),
            null);
        var sut = new UpdateTicketTypeHandler(Environment.Database.Context);

        // Act
        var result = await ErrorResult.CaptureAsync(
            async () => { await sut.HandleAsync(command, testContext.CancellationToken); });

        // Assert
        result.Error.ShouldMatch(TicketedEventLifecycleGuard.Errors.EventNotActive);
    }
}
