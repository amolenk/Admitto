using Amolenk.Admitto.Core.Badges.Application.UseCases.BadgeInstances.AddBadgeInstance;
using Amolenk.Admitto.Core.Badges.Domain.Entities;
using Amolenk.Admitto.Core.Shared.Kernel.ErrorHandling;
using Amolenk.Admitto.Testing.Infrastructure.Assertions;
using Should = Shouldly.Should;
using CoreBadgeInstanceId = Amolenk.Admitto.Core.Badges.Domain.ValueObjects.BadgeInstanceId;

namespace Amolenk.Admitto.Core.IntegrationTests.Badges.Application.UseCases.BadgeInstances.AddBadgeInstance;

[TestClass]
public sealed class AddBadgeInstanceTests(TestContext testContext) : AspireIntegrationTestBase
{
    // Given a standalone badge type on an active event
    // When a badge instance is added
    // Then it is persisted with the given display name and notes
    [TestMethod]
    public async ValueTask AddBadgeInstance_StandaloneType_CreatesInstance()
    {
        var fixture = AddBadgeInstanceFixture.StandaloneType();
        await fixture.SetupAsync(Environment);

        var command = new AddBadgeInstanceCommand(
            fixture.EventId,
            fixture.TeamId,
            fixture.BadgeTypeId,
            DisplayName: "Alice Smith",
            Notes: "Keynote");

        var sut = new AddBadgeInstanceHandler(Environment.BadgesDatabase.Context);

        var instanceId = await sut.HandleAsync(command, testContext.CancellationToken);

        await Environment.BadgesDatabase.AssertAsync(async db =>
        {
            var instance = await db.BadgeInstances.FindAsync(
                [CoreBadgeInstanceId.From(instanceId)], testContext.CancellationToken);

            instance.ShouldNotBeNull();
            instance.DisplayName.Value.ShouldBe("Alice Smith");
            instance.Notes.Value.ShouldBe("Keynote");
        });
    }

    // Given a ticket-based badge type on an active event
    // When a badge instance is added for it
    // Then it throws a business rule violation because only standalone types allow manual instances
    [TestMethod]
    public async ValueTask AddBadgeInstance_TicketBasedType_ThrowsNotStandaloneError()
    {
        var fixture = AddBadgeInstanceFixture.TicketBasedType();
        await fixture.SetupAsync(Environment);

        var command = new AddBadgeInstanceCommand(
            fixture.EventId,
            fixture.TeamId,
            fixture.BadgeTypeId,
            DisplayName: "Alice Smith",
            Notes: "");

        var sut = new AddBadgeInstanceHandler(Environment.BadgesDatabase.Context);

        var exception = await Should.ThrowAsync<BusinessRuleViolationException>(
            async () => await sut.HandleAsync(command, testContext.CancellationToken));

        exception.Error.ShouldMatch(BadgeEvent.Errors.NotStandaloneBadgeType);
    }
}
