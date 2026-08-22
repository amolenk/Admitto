using Amolenk.Admitto.Core.Badges.Domain.Entities;
using Amolenk.Admitto.Core.Badges.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;
using CoreBadgeInstanceId = Amolenk.Admitto.Core.Badges.Domain.ValueObjects.BadgeInstanceId;
using CoreBadgeTypeId = Amolenk.Admitto.Core.Badges.Domain.ValueObjects.BadgeTypeId;
using CoreTeamId = Amolenk.Admitto.Core.Shared.Kernel.ValueObjects.TeamId;

namespace Amolenk.Admitto.Core.IntegrationTests.Badges.Application.UseCases.BadgeTypes.GetBadgeTypes;

internal sealed class GetBadgeTypesFixture
{
    public Guid EventId { get; private set; }
    public Guid TeamId { get; private set; }
    public Guid StandaloneBadgeTypeId { get; private set; }
    public Guid TicketBasedBadgeTypeId { get; private set; }

    public static GetBadgeTypesFixture ActiveEventWithBothKinds() => new();

    public async ValueTask SetupAsync(IntegrationTestEnvironment environment)
    {
        var teamId = CoreTeamId.New();
        TeamId = teamId.Value;

        var eventId = TicketedEventId.New();
        EventId = eventId.Value;

        var badgesEvent = BadgeEvent.Create(eventId, teamId);

        var standaloneId = CoreBadgeTypeId.New();
        StandaloneBadgeTypeId = standaloneId.Value;
        badgesEvent.AddBadgeType(standaloneId, BadgeTypeName.From("Speaker Badge"), BadgeKind.Standalone, []);

        var ticketBasedId = CoreBadgeTypeId.New();
        TicketBasedBadgeTypeId = ticketBasedId.Value;
        badgesEvent.AddBadgeType(
            ticketBasedId, BadgeTypeName.From("Attendee Badge"), BadgeKind.TicketBased, [TicketTypeId.New()]);

        var instances = Enumerable.Range(1, 2)
            .Select(i => BadgeInstance.Create(
                CoreBadgeInstanceId.New(),
                teamId,
                eventId,
                standaloneId,
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
