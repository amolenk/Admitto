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
    public async ValueTask SC004_GetTicketedEvent_ExistingEvent_ReturnsEventWithTicketTypes()
    {
        // Arrange
        // SC-004: Given a ticketed event exists with two ticket types, when an organizer
        // requests the event by slug, the full event details including ticket types are returned.
        var fixture = GetTicketedEventFixture.EventWithTicketTypes();
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

        result.TicketTypes.Count.ShouldBe(2);

        var general = result.TicketTypes.FirstOrDefault(tt => tt.Slug == "general");
        general.ShouldNotBeNull();
        general.Name.ShouldBe("General Admission");
        general.Capacity.ShouldBe(500);
        general.IsCancelled.ShouldBeFalse();

        var vip = result.TicketTypes.FirstOrDefault(tt => tt.Slug == "vip");
        vip.ShouldNotBeNull();
        vip.Name.ShouldBe("VIP Pass");
        vip.Capacity.ShouldBe(50);
        vip.TimeSlots.Count.ShouldBe(2);
    }

    [TestMethod]
    public async ValueTask GetTicketedEvent_NonExistentEvent_ThrowsNotFound()
    {
        // Arrange
        var fixture = GetTicketedEventFixture.EventWithTicketTypes();
        await fixture.SetupAsync(Environment);

        var query = new GetTicketedEventQuery(fixture.TeamId, "does-not-exist");
        var sut = new GetTicketedEventHandler(Environment.Database.Context);

        // Act & Assert
        var exception = await Should.ThrowAsync<BusinessRuleViolationException>(
            async () => await sut.HandleAsync(query, testContext.CancellationToken));

        exception.Error.Code.ShouldBe(NotFoundError.Create<TicketedEvent>("does-not-exist").Code);
    }
}
