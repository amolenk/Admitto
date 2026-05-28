using Amolenk.Admitto.Core.Badges.Application.UseCases.BadgeTypes.AddBadgeType;
using Amolenk.Admitto.Core.Badges.Domain.Entities;
using Amolenk.Admitto.Core.Shared.Kernel.ErrorHandling;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;
using Amolenk.Admitto.Testing.Infrastructure.Assertions;

namespace Amolenk.Admitto.Core.IntegrationTests.Badges.Application.UseCases.BadgeTypes.AddBadgeType;

[TestClass]
public sealed class AddBadgeTypeTests(TestContext testContext) : AspireIntegrationTestBase
{
    [TestMethod]
    public async ValueTask AddBadgeType_WrongTeamId_ThrowsNotFoundError()
    {
        // Arrange: create badges event for team A
        var eventId = TicketedEventId.New();
        var teamIdA = TeamId.New();
        var teamIdB = TeamId.New();

        var badgesEvent = BadgesEvent.Create(eventId, teamIdA);

        await Environment.BadgesDatabase.SeedAsync(db => db.BadgesEvents.Add(badgesEvent));

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
        result.Error.ShouldMatch(NotFoundError.Create<BadgesEvent>());
    }
}
