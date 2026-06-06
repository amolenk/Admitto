using Amolenk.Admitto.Core.Badges.Domain.Entities;
using Amolenk.Admitto.Core.Badges.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;
using CoreBadgeTypeId = Amolenk.Admitto.Core.Badges.Domain.ValueObjects.BadgeTypeId;
using CoreTeamId = Amolenk.Admitto.Core.Shared.Kernel.ValueObjects.TeamId;

namespace Amolenk.Admitto.Core.IntegrationTests.Badges.Application.UseCases.BadgeTypes.RenameBadgeType;

internal sealed class RenameBadgeTypeFixture
{
    public Guid EventId { get; private set; }
    public Guid TeamId { get; private set; }
    public Guid BadgeTypeId { get; private set; }
    public uint BadgeTypeVersion { get; private set; }

    public static RenameBadgeTypeFixture ActiveEvent() => new();

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
            BadgeTypeName.From("Original Name"),
            BadgeKind.Standalone,
            []);

        await environment.BadgesDatabase.SeedAsync(db =>
        {
            db.BadgeEvents.Add(badgesEvent);
            db.BadgeTypes.Add(badgeType);
        });

        BadgeTypeVersion = badgeType.Version;
    }
}
