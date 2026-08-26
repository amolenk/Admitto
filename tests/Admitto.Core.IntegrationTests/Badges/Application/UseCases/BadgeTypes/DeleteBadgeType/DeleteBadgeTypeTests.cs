using Microsoft.EntityFrameworkCore;
using Amolenk.Admitto.Core.Badges.Application.UseCases.BadgeTypes.DeleteBadgeType;
using Amolenk.Admitto.Core.Badges.Domain.Entities;
using Amolenk.Admitto.Core.Shared.Kernel.ErrorHandling;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;
using Amolenk.Admitto.Testing.Infrastructure.Assertions;
using CoreBadgeTypeId = Amolenk.Admitto.Core.Badges.Domain.ValueObjects.BadgeTypeId;

namespace Amolenk.Admitto.Core.IntegrationTests.Badges.Application.UseCases.BadgeTypes.DeleteBadgeType;

[TestClass]
public sealed class DeleteBadgeTypeTests(TestContext testContext) : AspireIntegrationTestBase
{
    // Given a standalone badge type with existing badge instances
    // When the badge type is deleted
    // Then both the badge type and its instances are removed from the database
    [TestMethod]
    public async ValueTask DeleteBadgeType_StandaloneWithInstances_CascadeDeletesInstances()
    {
        var fixture = DeleteBadgeTypeFixture.StandaloneWithInstances(instanceCount: 2);
        await fixture.SetupAsync(Environment);

        var command = new DeleteBadgeTypeCommand(fixture.EventId, fixture.TeamId, fixture.BadgeTypeId);
        var sut = new DeleteBadgeTypeHandler(Environment.BadgesDatabase.Context);

        await sut.HandleAsync(command, testContext.CancellationToken);

        await Environment.BadgesDatabase.AssertAsync(async db =>
        {
            var badgeEvent = await db.BadgeEvents
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.Id == TicketedEventId.From(fixture.EventId), testContext.CancellationToken);

            badgeEvent.ShouldNotBeNull();
            badgeEvent.BadgeTypes.ShouldNotContain(bt => bt.Id.Value == fixture.BadgeTypeId);

            var badgeTypeId = CoreBadgeTypeId.From(fixture.BadgeTypeId);
            var remainingInstances = await db.BadgeInstances
                .AsNoTracking()
                .CountAsync(bi => bi.BadgeTypeId == badgeTypeId, testContext.CancellationToken);
            remainingInstances.ShouldBe(0);
        });
    }

    // Given a badge event that belongs to team A
    // When a badge type is deleted using team B's ID
    // Then the cross-team access is rejected with a not-found error
    [TestMethod]
    public async ValueTask DeleteBadgeType_WrongTeamId_ThrowsNotFoundError()
    {
        var fixture = DeleteBadgeTypeFixture.TicketBased();
        await fixture.SetupAsync(Environment);

        var command = new DeleteBadgeTypeCommand(fixture.EventId, TeamId.New().Value, fixture.BadgeTypeId);
        var sut = new DeleteBadgeTypeHandler(Environment.BadgesDatabase.Context);

        var result = await ErrorResult.CaptureAsync(
            async () => { await sut.HandleAsync(command, testContext.CancellationToken); });

        result.Error.ShouldMatch(NotFoundError.Create<BadgeEvent>());
    }
}
