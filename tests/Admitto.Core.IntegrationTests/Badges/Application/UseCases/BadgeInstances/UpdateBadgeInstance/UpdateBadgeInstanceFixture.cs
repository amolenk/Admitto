using Amolenk.Admitto.Core.Badges.Domain.Entities;
using Amolenk.Admitto.Core.Badges.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;
using CoreBadgeInstanceId = Amolenk.Admitto.Core.Badges.Domain.ValueObjects.BadgeInstanceId;
using CoreBadgeTypeId = Amolenk.Admitto.Core.Badges.Domain.ValueObjects.BadgeTypeId;
using CoreTeamId = Amolenk.Admitto.Core.Shared.Kernel.ValueObjects.TeamId;

namespace Amolenk.Admitto.Core.IntegrationTests.Badges.Application.UseCases.BadgeInstances.UpdateBadgeInstance;

internal sealed class UpdateBadgeInstanceFixture
{
    private readonly bool _instanceInDifferentEvent;

    public Guid EventId { get; private set; }
    public Guid TeamId { get; private set; }
    public Guid BadgeTypeId { get; private set; }
    public Guid BadgeInstanceId { get; private set; }
    public uint BadgeInstanceVersion { get; private set; }

    private UpdateBadgeInstanceFixture(bool instanceInDifferentEvent = false)
    {
        _instanceInDifferentEvent = instanceInDifferentEvent;
    }

    public static UpdateBadgeInstanceFixture ActiveEvent() => new();

    public static UpdateBadgeInstanceFixture InstanceInDifferentEvent() => new(instanceInDifferentEvent: true);

    public async ValueTask SetupAsync(IntegrationTestEnvironment environment)
    {
        var teamId = CoreTeamId.New();
        TeamId = teamId.Value;

        var eventId = TicketedEventId.New();
        EventId = eventId.Value;

        var badgesEvent = BadgeEvent.Create(eventId, teamId);

        var badgeTypeId = CoreBadgeTypeId.New();
        BadgeTypeId = badgeTypeId.Value;

        // Add badge type directly to the aggregate via AddBadgeType method
        badgesEvent.AddBadgeType(
            badgeTypeId,
            BadgeTypeName.From("Speaker Badge"),
            BadgeKind.Standalone,
            []);

        var badgeInstanceId = CoreBadgeInstanceId.New();
        BadgeInstanceId = badgeInstanceId.Value;

        var badgeInstance = BadgeInstance.Create(
            badgeInstanceId,
            teamId,
            _instanceInDifferentEvent ? TicketedEventId.New() : eventId,
            badgeTypeId,
            BadgeInstanceDisplayName.From("Alice Smith"),
            BadgeInstanceNotes.From(""));

        await environment.BadgesDatabase.SeedAsync(db =>
        {
            db.BadgeEvents.Add(badgesEvent);
            db.BadgeInstances.Add(badgeInstance);
        });

        BadgeInstanceVersion = badgeInstance.Version;
    }
}
