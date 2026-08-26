using Microsoft.EntityFrameworkCore;
using Amolenk.Admitto.Core.Badges.Application.UseCases.BadgeTypes.AddBadgeType;
using Amolenk.Admitto.Core.Badges.Domain.Entities;
using Amolenk.Admitto.Core.Badges.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Kernel.ErrorHandling;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;
using Amolenk.Admitto.Testing.Infrastructure.Assertions;

namespace Amolenk.Admitto.Core.IntegrationTests.Badges.Application.UseCases.BadgeTypes.AddBadgeType;

[TestClass]
public sealed class AddBadgeTypeTests(TestContext testContext) : AspireIntegrationTestBase
{
    // Given an active badge event
    // When a standalone badge type is added
    // Then it is persisted on the event with the given name and kind
    [TestMethod]
    public async ValueTask AddBadgeType_ValidInput_AddsBadgeTypeToEvent()
    {
        // Arrange
        var eventId = TicketedEventId.New();
        var teamId = TeamId.New();

        var badgesEvent = BadgeEvent.Create(eventId, teamId);

        await Environment.BadgesDatabase.SeedAsync(db => db.BadgeEvents.Add(badgesEvent));

        var command = new AddBadgeTypeCommand(
            EventId: eventId.Value,
            TeamId: teamId.Value,
            Name: "Speaker Badge",
            Kind: "Standalone",
            TicketTypeIds: []);

        var sut = new AddBadgeTypeHandler(Environment.BadgesDatabase.Context);

        // Act
        var badgeTypeId = await sut.HandleAsync(command, testContext.CancellationToken);

        // Assert
        await Environment.BadgesDatabase.AssertAsync(async db =>
        {
            var updatedEvent = await db.BadgeEvents
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.Id == eventId, testContext.CancellationToken);

            updatedEvent.ShouldNotBeNull();
            var badgeType = updatedEvent.BadgeTypes.FirstOrDefault(bt => bt.Id.Value == badgeTypeId);
            badgeType.ShouldNotBeNull();
            badgeType!.Name.Value.ShouldBe("Speaker Badge");
            badgeType.Kind.ShouldBe(BadgeKind.Standalone);
        });
    }

    // Given a badge event that belongs to team A
    // When a badge type is added using team B's ID
    // Then the cross-team access is rejected with a not-found error
    [TestMethod]
    public async ValueTask AddBadgeType_WrongTeamId_ThrowsNotFoundError()
    {
        // Arrange: create badges event for team A
        var eventId = TicketedEventId.New();
        var teamIdA = TeamId.New();
        var teamIdB = TeamId.New();

        var badgesEvent = BadgeEvent.Create(eventId, teamIdA);

        await Environment.BadgesDatabase.SeedAsync(db => db.BadgeEvents.Add(badgesEvent));

        // Act: try to add a badge type using team B's ID
        var command = new AddBadgeTypeCommand(
            EventId: eventId.Value,
            TeamId: teamIdB.Value,
            Name: "Speaker Badge",
            Kind: "QrCode",
            TicketTypeIds: []);

        var sut = new AddBadgeTypeHandler(Environment.BadgesDatabase.Context);

        var result = await ErrorResult.CaptureAsync(
            async () => { await sut.HandleAsync(command, testContext.CancellationToken); });

        // Assert: cross-team access is rejected
        result.Error.ShouldMatch(NotFoundError.Create<BadgeEvent>());
    }
}
