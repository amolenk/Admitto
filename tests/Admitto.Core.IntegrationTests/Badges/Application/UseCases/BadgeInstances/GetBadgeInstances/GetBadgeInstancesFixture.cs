using Amolenk.Admitto.Core.Badges.Domain.Entities;
using Amolenk.Admitto.Core.Badges.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;
using CoreBadgeInstanceId = Amolenk.Admitto.Core.Badges.Domain.ValueObjects.BadgeInstanceId;
using CoreBadgeTypeId = Amolenk.Admitto.Core.Badges.Domain.ValueObjects.BadgeTypeId;
using CoreTeamId = Amolenk.Admitto.Core.Shared.Kernel.ValueObjects.TeamId;

namespace Amolenk.Admitto.Core.IntegrationTests.Badges.Application.UseCases.BadgeInstances.GetBadgeInstances;

internal sealed class GetBadgeInstancesFixture
{
    private readonly BadgeKind _kind;

    public Guid EventId { get; private set; }
    public Guid TeamId { get; private set; }
    public Guid BadgeTypeId { get; private set; }

    private GetBadgeInstancesFixture(BadgeKind kind)
    {
        _kind = kind;
    }

    public static GetBadgeInstancesFixture StandaloneTypeWithInstances() => new(BadgeKind.Standalone);

    public static GetBadgeInstancesFixture TicketBasedType() => new(BadgeKind.TicketBased);

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

        if (_kind == BadgeKind.Standalone)
        {
            var instances = new[]
            {
                BadgeInstance.Create(
                    CoreBadgeInstanceId.New(), teamId, eventId, badgeTypeId,
                    BadgeInstanceDisplayName.From("Charlie Brown"), BadgeInstanceNotes.From("")),
                BadgeInstance.Create(
                    CoreBadgeInstanceId.New(), teamId, eventId, badgeTypeId,
                    BadgeInstanceDisplayName.From("Alice Smith"), BadgeInstanceNotes.From("")),
                BadgeInstance.Create(
                    CoreBadgeInstanceId.New(), teamId, eventId, badgeTypeId,
                    BadgeInstanceDisplayName.From("Bob Jones"), BadgeInstanceNotes.From("")),
            };

            await environment.BadgesDatabase.SeedAsync(db => db.BadgeInstances.AddRange(instances));
        }
    }
}
