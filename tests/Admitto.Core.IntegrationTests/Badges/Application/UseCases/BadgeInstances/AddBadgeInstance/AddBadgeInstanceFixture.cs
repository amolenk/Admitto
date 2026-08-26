using Amolenk.Admitto.Core.Badges.Domain.Entities;
using Amolenk.Admitto.Core.Badges.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;
using CoreBadgeTypeId = Amolenk.Admitto.Core.Badges.Domain.ValueObjects.BadgeTypeId;
using CoreTeamId = Amolenk.Admitto.Core.Shared.Kernel.ValueObjects.TeamId;

namespace Amolenk.Admitto.Core.IntegrationTests.Badges.Application.UseCases.BadgeInstances.AddBadgeInstance;

internal sealed class AddBadgeInstanceFixture
{
    private readonly BadgeKind _kind;

    public Guid EventId { get; private set; }
    public Guid TeamId { get; private set; }
    public Guid BadgeTypeId { get; private set; }

    private AddBadgeInstanceFixture(BadgeKind kind)
    {
        _kind = kind;
    }

    public static AddBadgeInstanceFixture StandaloneType() => new(BadgeKind.Standalone);

    public static AddBadgeInstanceFixture TicketBasedType() => new(BadgeKind.TicketBased);

    public async ValueTask SetupAsync(IntegrationTestEnvironment environment)
    {
        var teamId = CoreTeamId.New();
        TeamId = teamId.Value;

        var eventId = TicketedEventId.New();
        EventId = eventId.Value;

        var badgesEvent = BadgeEvent.Create(eventId, teamId);

        var badgeTypeId = CoreBadgeTypeId.New();
        BadgeTypeId = badgeTypeId.Value;

        badgesEvent.AddBadgeType(
            badgeTypeId,
            BadgeTypeName.From("Speaker Badge"),
            _kind,
            _kind == BadgeKind.TicketBased ? [TicketTypeId.New()] : []);

        await environment.BadgesDatabase.SeedAsync(db => db.BadgeEvents.Add(badgesEvent));
    }
}
