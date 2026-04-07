using Amolenk.Admitto.Module.Registrations.Application.UseCases.TicketTypeManagement.CancelTicketType;
using Amolenk.Admitto.Module.Registrations.Tests.Application.Aspire;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;
using Amolenk.Admitto.Testing.Infrastructure.Assertions;
using Microsoft.EntityFrameworkCore;

namespace Amolenk.Admitto.Module.Registrations.Tests.Application.UseCases.TicketTypeManagement.CancelTicketType;

[TestClass]
public sealed class CancelTicketTypeTests(TestContext testContext) : AspireIntegrationTestBase
{
    // SC-001: Cancel active ticket type — succeeds, IsCancelled is true
    [TestMethod]
    public async ValueTask SC001_CancelTicketType_ActiveTicketType_SetsIsCancelledTrue()
    {
        // Arrange
        var fixture = CancelTicketTypeFixture.ActiveEvent();
        await fixture.SetupAsync(Environment);

        var command = new CancelTicketTypeCommand(
            fixture.EventId,
            Slug.From(fixture.TicketTypeSlug));
        var sut = new CancelTicketTypeHandler(Environment.Database.Context);

        // Act
        await sut.HandleAsync(command, testContext.CancellationToken);

        // Assert
        await Environment.Database.AssertAsync(async dbContext =>
        {
            var catalog = await dbContext.TicketCatalogs
                .FirstOrDefaultAsync(tc => tc.Id == fixture.EventId, testContext.CancellationToken);

            catalog.ShouldNotBeNull();
            var ticketType = catalog.TicketTypes.ShouldHaveSingleItem();
            ticketType.IsCancelled.ShouldBeTrue();
        });
    }

    // SC-002: Reject double-cancel — throws BusinessRuleViolationException
    [TestMethod]
    public async ValueTask SC002_CancelTicketType_AlreadyCancelled_ThrowsAlreadyCancelledError()
    {
        // Arrange
        var fixture = CancelTicketTypeFixture.AlreadyCancelled();
        await fixture.SetupAsync(Environment);

        var command = new CancelTicketTypeCommand(
            fixture.EventId,
            Slug.From(fixture.TicketTypeSlug));
        var sut = new CancelTicketTypeHandler(Environment.Database.Context);

        // Act
        var result = await ErrorResult.CaptureAsync(
            async () => { await sut.HandleAsync(command, testContext.CancellationToken); });

        // Assert
        result.Error.Code.ShouldBe("ticket_type.already_cancelled");
    }

    // SC-003: Reject on cancelled event — throws BusinessRuleViolationException
    [TestMethod]
    public async ValueTask SC003_CancelTicketType_CancelledEvent_ThrowsEventNotActiveError()
    {
        // Arrange
        var fixture = CancelTicketTypeFixture.CancelledEvent();
        await fixture.SetupAsync(Environment);

        var command = new CancelTicketTypeCommand(
            fixture.EventId,
            Slug.From(fixture.TicketTypeSlug));
        var sut = new CancelTicketTypeHandler(Environment.Database.Context);

        // Act
        var result = await ErrorResult.CaptureAsync(
            async () => { await sut.HandleAsync(command, testContext.CancellationToken); });

        // Assert
        result.Error.ShouldMatch(CancelTicketTypeHandler.Errors.EventNotActive);
    }
}
