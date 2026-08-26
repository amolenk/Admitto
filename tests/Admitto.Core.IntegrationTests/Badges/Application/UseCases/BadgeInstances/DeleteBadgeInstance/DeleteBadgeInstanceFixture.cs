using Amolenk.Admitto.Core.Badges.Domain.Entities;
using Amolenk.Admitto.Core.Badges.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;
using CoreBadgeInstanceId = Amolenk.Admitto.Core.Badges.Domain.ValueObjects.BadgeInstanceId;
using CoreBadgeTypeId = Amolenk.Admitto.Core.Badges.Domain.ValueObjects.BadgeTypeId;
using CoreTeamId = Amolenk.Admitto.Core.Shared.Kernel.ValueObjects.TeamId;

namespace Amolenk.Admitto.Core.IntegrationTests.Badges.Application.UseCases.BadgeInstances.DeleteBadgeInstance;

internal sealed class DeleteBadgeInstanceFixture
{
    private readonly bool _eventArchived;

    public Guid EventId { get; private set; }
    public Guid TeamId { get; private set; }
    public Guid BadgeTypeId { get; private set; }
    public Guid BadgeInstanceId { get; private set; }

    private DeleteBadgeInstanceFixture(bool eventArchived)
    {
        _eventArchived = eventArchived;
    }

    public static DeleteBadgeInstanceFixture ActiveEvent() => new(eventArchived: false);

    public static DeleteBadgeInstanceFixture ArchivedEvent() => new(eventArchived: true);

    public async ValueTask SetupAsync(IntegrationTestEnvironment environment)
    {
        var teamId = CoreTeamId.New();
        TeamId = teamId.Value;

        var eventId = TicketedEventId.New();
        EventId = eventId.Value;

        var badgesEvent = BadgeEvent.Create(eventId, teamId);

        var badgeTypeId = CoreBadgeTypeId.New();
        BadgeTypeId = badgeTypeId.Value;

        badgesEvent.AddBadgeType(badgeTypeId, BadgeTypeName.From("Speaker Badge"), BadgeKind.Standalone, []);

        if (_eventArchived)
        {
            badgesEvent.MarkArchived();
        }

        var badgeInstanceId = CoreBadgeInstanceId.New();
        BadgeInstanceId = badgeInstanceId.Value;

        var badgeInstance = BadgeInstance.Create(
            badgeInstanceId,
            teamId,
            eventId,
            badgeTypeId,
            BadgeInstanceDisplayName.From("Alice Smith"),
            BadgeInstanceNotes.From(""));

        await environment.BadgesDatabase.SeedAsync(db =>
        {
            db.BadgeEvents.Add(badgesEvent);
            db.BadgeInstances.Add(badgeInstance);
        });
    }
}
