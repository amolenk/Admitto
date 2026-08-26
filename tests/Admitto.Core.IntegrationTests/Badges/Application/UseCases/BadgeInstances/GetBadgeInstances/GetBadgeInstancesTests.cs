using Amolenk.Admitto.Core.Badges.Application.UseCases.BadgeInstances.GetBadgeInstances;
using Amolenk.Admitto.Core.Badges.Domain.Entities;
using Amolenk.Admitto.Core.Shared.Kernel.ErrorHandling;
using Amolenk.Admitto.Testing.Infrastructure.Assertions;
using Should = Shouldly.Should;

namespace Amolenk.Admitto.Core.IntegrationTests.Badges.Application.UseCases.BadgeInstances.GetBadgeInstances;

[TestClass]
public sealed class GetBadgeInstancesTests(TestContext testContext) : AspireIntegrationTestBase
{
    // Given a standalone badge type with badge instances added out of alphabetical order
    // When the badge instances are queried
    // Then they are returned ordered by display name ascending
    [TestMethod]
    public async ValueTask GetBadgeInstances_StandaloneType_ReturnsOrderedByDisplayName()
    {
        var fixture = GetBadgeInstancesFixture.StandaloneTypeWithInstances();
        await fixture.SetupAsync(Environment);

        var query = new GetBadgeInstancesQuery(fixture.EventId, fixture.TeamId, fixture.BadgeTypeId);
        var sut = new GetBadgeInstancesHandler(Environment.BadgesDatabase.Context);

        var instances = await sut.HandleAsync(query, testContext.CancellationToken);

        instances.Select(i => i.DisplayName).ShouldBe(["Alice Smith", "Bob Jones", "Charlie Brown"]);
    }

    // Given a ticket-based badge type
    // When its badge instances are queried
    // Then it throws a business rule violation because only standalone types have manually managed instances
    [TestMethod]
    public async ValueTask GetBadgeInstances_TicketBasedType_ThrowsNotStandaloneError()
    {
        var fixture = GetBadgeInstancesFixture.TicketBasedType();
        await fixture.SetupAsync(Environment);

        var query = new GetBadgeInstancesQuery(fixture.EventId, fixture.TeamId, fixture.BadgeTypeId);
        var sut = new GetBadgeInstancesHandler(Environment.BadgesDatabase.Context);

        var exception = await Should.ThrowAsync<BusinessRuleViolationException>(
            async () => await sut.HandleAsync(query, testContext.CancellationToken));

        exception.Error.ShouldMatch(BadgeEvent.Errors.NotStandaloneBadgeType);
    }
}
