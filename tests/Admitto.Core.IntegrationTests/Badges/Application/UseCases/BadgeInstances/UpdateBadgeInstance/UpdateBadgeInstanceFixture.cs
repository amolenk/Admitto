using Amolenk.Admitto.Core.Badges.Domain.Entities;
using Amolenk.Admitto.Core.Badges.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;
using CoreBadgeInstanceId = Amolenk.Admitto.Core.Badges.Domain.ValueObjects.BadgeInstanceId;
using CoreBadgeTypeId = Amolenk.Admitto.Core.Badges.Domain.ValueObjects.BadgeTypeId;
using CoreTeamId = Amolenk.Admitto.Core.Shared.Kernel.ValueObjects.TeamId;

namespace Amolenk.Admitto.Core.IntegrationTests.Badges.Application.UseCases.BadgeInstances.UpdateBadgeInstance;

internal sealed class UpdateBadgeInstanceFixture
{
    public Guid EventId { get; private set; }
    public Guid TeamId { get; private set; }
    public Guid BadgeTypeId { get; private set; }
    public Guid BadgeInstanceId { get; private set; }
    public uint BadgeInstanceVersion { get; private set; }

    public static UpdateBadgeInstanceFixture ActiveEvent() => new();

    public async ValueTask SetupAsync(IntegrationTestEnvironment environment)
    {
        var teamId = CoreTeamId.New();
        TeamId = teamId.Value;

        var eventId = TicketedEventId.New();
        EventId = eventId.Value;

        var badgesEvent = BadgeEvent.Create(eventId, teamId);

        var badgeTypeId = CoreBadgeTypeId.New();
        BadgeTypeId = badgeTypeId.Value;

        var badgeType = BadgeType.Create(
            badgeTypeId,
            eventId,
            BadgeTypeName.From("Speaker Badge"),
            BadgeKind.Standalone,
            []);

        var badgeInstanceId = CoreBadgeInstanceId.New();
        BadgeInstanceId = badgeInstanceId.Value;

        var badgeInstance = BadgeInstance.Create(
            badgeInstanceId,
            badgeTypeId,
            BadgeInstanceDisplayName.From("Alice Smith"),
            BadgeInstanceNotes.From(""));

        await environment.BadgesDatabase.SeedAsync(db =>
        {
            db.BadgeEvents.Add(badgesEvent);
            db.BadgeTypes.Add(badgeType);
            db.BadgeInstances.Add(badgeInstance);
        });

        BadgeInstanceVersion = badgeInstance.Version;
    }
}
