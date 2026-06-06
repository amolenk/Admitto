using Amolenk.Admitto.Core.Badges.Application.UseCases.BadgeInstances.UpdateBadgeInstance;
using Amolenk.Admitto.Core.Shared.Kernel.ErrorHandling;
using Amolenk.Admitto.Testing.Infrastructure.Assertions;
using Should = Shouldly.Should;

namespace Amolenk.Admitto.Core.IntegrationTests.Badges.Application.UseCases.BadgeInstances.UpdateBadgeInstance;

[TestClass]
public sealed class UpdateBadgeInstanceTests(TestContext testContext) : AspireIntegrationTestBase
{
    [TestMethod]
    public async ValueTask UpdateBadgeInstance_WithCorrectVersion_UpdatesInstance()
    {
        var fixture = UpdateBadgeInstanceFixture.ActiveEvent();
        await fixture.SetupAsync(Environment);

        var command = new UpdateBadgeInstanceCommand(
            fixture.EventId,
            fixture.TeamId,
            fixture.BadgeTypeId,
            fixture.BadgeInstanceId,
            DisplayName: "Alice Smith (Updated)",
            Notes: "Workshop",
            ExpectedVersion: fixture.BadgeInstanceVersion);

        var sut = new UpdateBadgeInstanceHandler(Environment.BadgesDatabase.Context);

        await sut.HandleAsync(command, testContext.CancellationToken);

        await Environment.BadgesDatabase.AssertAsync(async db =>
        {
            var instance = await db.BadgeInstances.FindAsync(
                [Amolenk.Admitto.Core.Badges.Domain.ValueObjects.BadgeInstanceId.From(fixture.BadgeInstanceId)],
                testContext.CancellationToken);

            instance.ShouldNotBeNull();
            instance.DisplayName.Value.ShouldBe("Alice Smith (Updated)");
            instance.Notes.Value.ShouldBe("Workshop");
            instance.Version.ShouldBeGreaterThan(fixture.BadgeInstanceVersion);
        });
    }

    [TestMethod]
    public async ValueTask UpdateBadgeInstance_WithStaleVersion_ThrowsConcurrencyConflict()
    {
        var fixture = UpdateBadgeInstanceFixture.ActiveEvent();
        await fixture.SetupAsync(Environment);

        var wrongVersion = fixture.BadgeInstanceVersion > 0 ? 0u : uint.MaxValue;

        var command = new UpdateBadgeInstanceCommand(
            fixture.EventId,
            fixture.TeamId,
            fixture.BadgeTypeId,
            fixture.BadgeInstanceId,
            DisplayName: "Alice Smith (Updated)",
            Notes: "Workshop",
            ExpectedVersion: wrongVersion);

        var sut = new UpdateBadgeInstanceHandler(Environment.BadgesDatabase.Context);

        var exception = await Should.ThrowAsync<BusinessRuleViolationException>(
            async () => await sut.HandleAsync(command, testContext.CancellationToken));

        exception.Error.ShouldMatch(ConcurrencyConflictError.Create(wrongVersion, fixture.BadgeInstanceVersion));
    }
}
