using Amolenk.Admitto.Module.Organization.Tests.Application.Infrastructure;
using Amolenk.Admitto.Module.Organization.Application.UseCases.TicketedEvents.GetTicketedEvent;
using Amolenk.Admitto.Module.Organization.Domain.Entities;
using Amolenk.Admitto.Module.Shared.Kernel.ErrorHandling;
using Should = Shouldly.Should;
using Shouldly;

namespace Amolenk.Admitto.Module.Organization.Tests.Application.UseCases.TicketedEvents.GetTicketedEvent;

[TestClass]
public sealed class GetTicketedEventTests(TestContext testContext) : AspireIntegrationTestBase
{
    [TestMethod]
    public async ValueTask SC004_GetTicketedEvent_ExistingEvent_ReturnsEventDetails()
    {
        // Arrange
        // SC-004: Given a ticketed event exists, when an organizer
        // requests the event by slug, the event details are returned.
        var fixture = GetTicketedEventFixture.ActiveEvent();
        await fixture.SetupAsync(Environment);

        var query = new GetTicketedEventQuery(fixture.TeamId, fixture.EventSlug);
        var sut = new GetTicketedEventHandler(Environment.Database.Context);

        // Act
        var result = await sut.HandleAsync(query, testContext.CancellationToken);

        // Assert
        result.ShouldNotBeNull();
        result.Slug.ShouldBe(fixture.EventSlug);
        result.Name.ShouldBe("Acme Conf 2026");
        result.Status.ShouldBe("Active");
        result.Version.ShouldBeGreaterThan(0u);
    }

    [TestMethod]
    public async ValueTask GetTicketedEvent_NonExistentEvent_ThrowsNotFound()
    {
        // Arrange
        var fixture = GetTicketedEventFixture.ActiveEvent();
        await fixture.SetupAsync(Environment);

        var query = new GetTicketedEventQuery(fixture.TeamId, "does-not-exist");
        var sut = new GetTicketedEventHandler(Environment.Database.Context);

        // Act & Assert
        var exception = await Should.ThrowAsync<BusinessRuleViolationException>(
            async () => await sut.HandleAsync(query, testContext.CancellationToken));

        exception.Error.Code.ShouldBe(NotFoundError.Create<TicketedEvent>("does-not-exist").Code);
    }
}
