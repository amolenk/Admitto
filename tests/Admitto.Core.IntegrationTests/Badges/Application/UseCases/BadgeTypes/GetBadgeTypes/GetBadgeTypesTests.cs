using Amolenk.Admitto.Core.Badges.Application.UseCases.BadgeTypes.GetBadgeTypes;

namespace Amolenk.Admitto.Core.IntegrationTests.Badges.Application.UseCases.BadgeTypes.GetBadgeTypes;

[TestClass]
public sealed class GetBadgeTypesTests(TestContext testContext) : AspireIntegrationTestBase
{
    // Given an event with a standalone badge type that has two instances and a ticket-based badge type
    // When the badge types are queried
    // Then the standalone type's instance count reflects its instances and the ticket-based type's count is zero
    [TestMethod]
    public async ValueTask GetBadgeTypes_StandaloneAndTicketBasedTypes_ReturnsCorrectInstanceCounts()
    {
        var fixture = GetBadgeTypesFixture.ActiveEventWithBothKinds();
        await fixture.SetupAsync(Environment);

        var query = new GetBadgeTypesQuery(fixture.EventId, fixture.TeamId);
        var sut = new GetBadgeTypesHandler(Environment.BadgesDatabase.Context);

        var response = await sut.HandleAsync(query, testContext.CancellationToken);

        var standalone = response.BadgeTypes.Single(bt => bt.Id == fixture.StandaloneBadgeTypeId);
        standalone.InstanceCount.ShouldBe(2);

        var ticketBased = response.BadgeTypes.Single(bt => bt.Id == fixture.TicketBasedBadgeTypeId);
        ticketBased.InstanceCount.ShouldBe(0);
    }
}
