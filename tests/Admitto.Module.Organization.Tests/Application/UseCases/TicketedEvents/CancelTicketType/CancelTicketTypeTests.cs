using Amolenk.Admitto.Module.Organization.Tests.Application.Infrastructure;
using Amolenk.Admitto.Module.Organization.Application.UseCases.TicketedEvents.CancelTicketType;
using Amolenk.Admitto.Module.Organization.Domain.Entities;
using Amolenk.Admitto.Module.Shared.Kernel.ErrorHandling;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;
using Amolenk.Admitto.Testing.Infrastructure.Assertions;
using Should = Shouldly.Should;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace Amolenk.Admitto.Module.Organization.Tests.Application.UseCases.TicketedEvents.CancelTicketType;

[TestClass]
public sealed class CancelTicketTypeTests(TestContext testContext) : AspireIntegrationTestBase
{
    [TestMethod]
    public async ValueTask SC019_CancelTicketType_ActiveTicketType_MarksAsCancelled()
    {
        // Arrange
        // SC-019: Given an active ticketed event with an active ticket type, when an organizer
        // cancels the ticket type, it is marked as cancelled without affecting the event status.
        var fixture = CancelTicketTypeFixture.ActiveEventWithTicketType();
        await fixture.SetupAsync(Environment);

        var command = new CancelTicketTypeCommand(
            fixture.TeamId, fixture.EventId, fixture.TicketTypeSlug, fixture.EventVersion);

        var sut = new CancelTicketTypeHandler(Environment.Database.Context);

        // Act
        await sut.HandleAsync(command, testContext.CancellationToken);

        // Assert
        await Environment.Database.AssertAsync(async dbContext =>
        {
            var ticketedEvent = await dbContext.TicketedEvents.FindAsync(
                [TicketedEventId.From(fixture.EventId)],
                testContext.CancellationToken);

            ticketedEvent.ShouldNotBeNull();
            ticketedEvent.Status.ShouldBe(Domain.ValueObjects.EventStatus.Active);

            var ticketType = ticketedEvent.TicketTypes.Single(tt => tt.Slug.Value == fixture.TicketTypeSlug);
            ticketType.IsCancelled.ShouldBeTrue();
        });
    }

    [TestMethod]
    public async ValueTask SC020_CancelTicketType_AlreadyCancelledTicketType_ThrowsAlreadyCancelled()
    {
        // Arrange
        // SC-020: Given an active event with an already-cancelled ticket type, when an organizer
        // attempts to cancel it again, the request is rejected.
        var fixture = CancelTicketTypeFixture.ActiveEventWithCancelledTicketType();
        await fixture.SetupAsync(Environment);

        var command = new CancelTicketTypeCommand(
            fixture.TeamId, fixture.EventId, fixture.TicketTypeSlug, fixture.EventVersion);

        var sut = new CancelTicketTypeHandler(Environment.Database.Context);

        // Act & Assert
        var exception = await Should.ThrowAsync<BusinessRuleViolationException>(
            async () => await sut.HandleAsync(command, testContext.CancellationToken));

        exception.Error.ShouldMatch(
            TicketedEvent.Errors.TicketTypeAlreadyCancelled(Slug.From(fixture.TicketTypeSlug)));
    }
}
