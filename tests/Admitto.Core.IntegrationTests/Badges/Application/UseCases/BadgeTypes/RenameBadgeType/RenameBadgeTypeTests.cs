using Microsoft.EntityFrameworkCore;
using Amolenk.Admitto.Core.Badges.Application.UseCases.BadgeTypes.RenameBadgeType;
using Amolenk.Admitto.Core.Badges.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Kernel.ErrorHandling;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;
using Amolenk.Admitto.Testing.Infrastructure.Assertions;
using Should = Shouldly.Should;

namespace Amolenk.Admitto.Core.IntegrationTests.Badges.Application.UseCases.BadgeTypes.RenameBadgeType;

[TestClass]
public sealed class RenameBadgeTypeTests(TestContext testContext) : AspireIntegrationTestBase
{
    [TestMethod]
    public async ValueTask RenameBadgeType_WithCorrectVersion_RenamesBadgeType()
    {
        var fixture = RenameBadgeTypeFixture.ActiveEvent();
        await fixture.SetupAsync(Environment);

        var command = new RenameBadgeTypeCommand(
            fixture.EventId,
            fixture.TeamId,
            fixture.BadgeTypeId,
            Name: "Renamed",
            ExpectedVersion: fixture.BadgeTypeVersion);

        var sut = new RenameBadgeTypeHandler(Environment.BadgesDatabase.Context);

        await sut.HandleAsync(command, testContext.CancellationToken);

        // Verify the rename was successful by querying the event
        await Environment.BadgesDatabase.AssertAsync(async db =>
        {
            var badgeEvent = await db.BadgeEvents
                .AsNoTracking()
                .Where(e => e.Id == TicketedEventId.From(fixture.EventId))
                .FirstOrDefaultAsync(testContext.CancellationToken);

            badgeEvent.ShouldNotBeNull();
            var badgeType = badgeEvent.BadgeTypes.FirstOrDefault(bt => bt.Id.Value == fixture.BadgeTypeId);
            badgeType.ShouldNotBeNull();
            badgeType!.Name.Value.ShouldBe("Renamed");
            badgeEvent.Version.ShouldBeGreaterThan(fixture.BadgeTypeVersion);
        });
    }

    [TestMethod]
    public async ValueTask RenameBadgeType_WithStaleVersion_ThrowsConcurrencyConflict()
    {
        var fixture = RenameBadgeTypeFixture.ActiveEvent();
        await fixture.SetupAsync(Environment);

        var wrongVersion = fixture.BadgeTypeVersion > 0 ? 0u : uint.MaxValue;

        var command = new RenameBadgeTypeCommand(
            fixture.EventId,
            fixture.TeamId,
            fixture.BadgeTypeId,
            Name: "Renamed",
            ExpectedVersion: wrongVersion);

        var sut = new RenameBadgeTypeHandler(Environment.BadgesDatabase.Context);

        var exception = await Should.ThrowAsync<BusinessRuleViolationException>(
            async () => await sut.HandleAsync(command, testContext.CancellationToken));

        exception.Error.ShouldMatch(ConcurrencyConflictError.Create(wrongVersion, fixture.BadgeTypeVersion));
    }
}
