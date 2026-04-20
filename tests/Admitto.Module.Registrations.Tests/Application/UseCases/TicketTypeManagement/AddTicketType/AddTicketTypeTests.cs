using Amolenk.Admitto.Module.Registrations.Application.UseCases.TicketTypeManagement.AddTicketType;
using Amolenk.Admitto.Module.Registrations.Domain.Entities;
using Amolenk.Admitto.Module.Registrations.Tests.Application.Aspire;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;
using Amolenk.Admitto.Testing.Infrastructure.Assertions;
using Microsoft.EntityFrameworkCore;

namespace Amolenk.Admitto.Module.Registrations.Tests.Application.UseCases.TicketTypeManagement.AddTicketType;

[TestClass]
public sealed class AddTicketTypeTests(TestContext testContext) : AspireIntegrationTestBase
{
    // SC-001: Add ticket type to active event — succeeds, creates catalog and ticket type
    [TestMethod]
    public async ValueTask SC001_AddTicketType_ActiveEvent_CreatesCatalogAndTicketType()
    {
        // Arrange
        var fixture = AddTicketTypeFixture.ActiveEvent();
        await fixture.SetupAsync(Environment);

        var command = new AddTicketTypeCommand(
            fixture.EventId,
            Slug.From("general-admission"),
            DisplayName.From("General Admission"),
            ["morning"],
            100);
        var sut = new AddTicketTypeHandler(Environment.Database.Context);

        // Act
        await sut.HandleAsync(command, testContext.CancellationToken);

        // Assert
        await Environment.Database.AssertAsync(async dbContext =>
        {
            var catalog = await dbContext.TicketCatalogs
                .FirstOrDefaultAsync(tc => tc.Id == fixture.EventId, testContext.CancellationToken);

            catalog.ShouldNotBeNull();
            catalog.TicketTypes.ShouldHaveSingleItem();

            var ticketType = catalog.TicketTypes[0];
            ticketType.Id.ShouldBe("general-admission");
            ticketType.Name.Value.ShouldBe("General Admission");
            ticketType.TimeSlotSlugs.ShouldContain("morning");
            ticketType.MaxCapacity.ShouldBe(100);
            ticketType.IsCancelled.ShouldBeFalse();
        });
    }

    // SC-002: Add ticket type with no max capacity (null) — succeeds
    [TestMethod]
    public async ValueTask SC002_AddTicketType_NullMaxCapacity_Succeeds()
    {
        // Arrange
        var fixture = AddTicketTypeFixture.ActiveEvent();
        await fixture.SetupAsync(Environment);

        var command = new AddTicketTypeCommand(
            fixture.EventId,
            Slug.From("speaker-pass"),
            DisplayName.From("Speaker Pass"),
            [],
            null);
        var sut = new AddTicketTypeHandler(Environment.Database.Context);

        // Act
        await sut.HandleAsync(command, testContext.CancellationToken);

        // Assert
        await Environment.Database.AssertAsync(async dbContext =>
        {
            var catalog = await dbContext.TicketCatalogs
                .FirstOrDefaultAsync(tc => tc.Id == fixture.EventId, testContext.CancellationToken);

            catalog.ShouldNotBeNull();
            var ticketType = catalog.TicketTypes.ShouldHaveSingleItem();
            ticketType.MaxCapacity.ShouldBeNull();
        });
    }

    // SC-003: Reject duplicate slug — throws BusinessRuleViolationException
    [TestMethod]
    public async ValueTask SC003_AddTicketType_DuplicateSlug_ThrowsDuplicateSlugError()
    {
        // Arrange
        var fixture = AddTicketTypeFixture.ActiveEventWithCatalog();
        await fixture.SetupAsync(Environment);

        var command = new AddTicketTypeCommand(
            fixture.EventId,
            Slug.From("existing-type"),
            DisplayName.From("Duplicate"),
            [],
            50);
        var sut = new AddTicketTypeHandler(Environment.Database.Context);

        // Act
        var result = await ErrorResult.CaptureAsync(
            async () => { await sut.HandleAsync(command, testContext.CancellationToken); });

        // Assert
        result.Error.Code.ShouldBe("ticket_catalog.duplicate_slug");
    }

    // SC-004: Reject when event is cancelled — throws BusinessRuleViolationException
    [TestMethod]
    public async ValueTask SC004_AddTicketType_CancelledEvent_ThrowsEventNotActiveError()
    {
        // Arrange
        var fixture = AddTicketTypeFixture.CancelledEvent();
        await fixture.SetupAsync(Environment);

        var command = new AddTicketTypeCommand(
            fixture.EventId,
            Slug.From("general-admission"),
            DisplayName.From("General Admission"),
            [],
            100);
        var sut = new AddTicketTypeHandler(Environment.Database.Context);

        // Act
        var result = await ErrorResult.CaptureAsync(
            async () => { await sut.HandleAsync(command, testContext.CancellationToken); });

        // Assert
        result.Error.ShouldMatch(TicketedEventLifecycleGuard.Errors.EventNotActive);
    }
}
