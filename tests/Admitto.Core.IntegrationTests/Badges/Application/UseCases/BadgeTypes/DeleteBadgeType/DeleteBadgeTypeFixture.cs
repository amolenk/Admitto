using Amolenk.Admitto.Core.Badges.Domain.Entities;
using Amolenk.Admitto.Core.Badges.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;
using CoreBadgeInstanceId = Amolenk.Admitto.Core.Badges.Domain.ValueObjects.BadgeInstanceId;
using CoreBadgeTypeId = Amolenk.Admitto.Core.Badges.Domain.ValueObjects.BadgeTypeId;
using CoreTeamId = Amolenk.Admitto.Core.Shared.Kernel.ValueObjects.TeamId;

namespace Amolenk.Admitto.Core.IntegrationTests.Badges.Application.UseCases.BadgeTypes.DeleteBadgeType;

internal sealed class DeleteBadgeTypeFixture
{
    private readonly BadgeKind _kind;
    private readonly int _instanceCount;

    public Guid EventId { get; private set; }
    public Guid TeamId { get; private set; }
    public Guid BadgeTypeId { get; private set; }

    private DeleteBadgeTypeFixture(BadgeKind kind, int instanceCount)
    {
        _kind = kind;
        _instanceCount = instanceCount;
    }

    public static DeleteBadgeTypeFixture StandaloneWithInstances(int instanceCount)
        => new(BadgeKind.Standalone, instanceCount);

    public static DeleteBadgeTypeFixture TicketBased() => new(BadgeKind.TicketBased, instanceCount: 0);

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

        var instances = Enumerable.Range(1, _instanceCount)
            .Select(i => BadgeInstance.Create(
                CoreBadgeInstanceId.New(),
                teamId,
                eventId,
                badgeTypeId,
                BadgeInstanceDisplayName.From($"Attendee {i}"),
                BadgeInstanceNotes.From("")))
            .ToList();

        await environment.BadgesDatabase.SeedAsync(db =>
        {
            db.BadgeEvents.Add(badgesEvent);
            db.BadgeInstances.AddRange(instances);
        });
    }
}
