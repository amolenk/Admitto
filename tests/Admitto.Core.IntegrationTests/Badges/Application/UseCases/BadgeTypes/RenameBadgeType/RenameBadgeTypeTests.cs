using Amolenk.Admitto.Core.Badges.Application.UseCases.BadgeTypes.RenameBadgeType;
using Amolenk.Admitto.Core.Shared.Kernel.ErrorHandling;
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

        await Environment.BadgesDatabase.AssertAsync(async db =>
        {
            var badgeType = await db.BadgeTypes.FindAsync(
                [Amolenk.Admitto.Core.Badges.Domain.ValueObjects.BadgeTypeId.From(fixture.BadgeTypeId)],
                testContext.CancellationToken);

            badgeType.ShouldNotBeNull();
            badgeType.Name.Value.ShouldBe("Renamed");
            badgeType.Version.ShouldBeGreaterThan(fixture.BadgeTypeVersion);
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
