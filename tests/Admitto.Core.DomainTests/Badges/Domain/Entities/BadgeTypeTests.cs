using Amolenk.Admitto.Core.Badges.Domain.Entities;
using Amolenk.Admitto.Core.Badges.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Kernel.ErrorHandling;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;
using Shouldly;

namespace Amolenk.Admitto.Core.Badges.Domain.Tests.Entities;

[TestClass]
public sealed class BadgeTypeTests
{
    private static readonly TicketedEventId DefaultEventId = TicketedEventId.New();
    private static readonly TeamId DefaultTeamId = TeamId.New();
    private static readonly BadgeTypeName DefaultName = BadgeTypeName.From("Speaker");
    private static readonly TicketTypeId DefaultTicketTypeId = TicketTypeId.New();

    // ── Create ───────────────────────────────────────────────────────────────

    [TestMethod]
    public void Create_TicketBased_WithTicketTypeIds_Succeeds()
    {
        var aggregate = BadgeEvent.Create(DefaultEventId, DefaultTeamId);
        var badgeTypeId = aggregate.AddBadgeType(DefaultName, BadgeKind.TicketBased, [DefaultTicketTypeId]);

        var badgeType = aggregate.BadgeTypes.First(bt => bt.Id == badgeTypeId);
        badgeType.Id.ShouldBe(badgeTypeId);
        badgeType.Name.ShouldBe(DefaultName);
        badgeType.Kind.ShouldBe(BadgeKind.TicketBased);
        badgeType.TicketTypeIds.ShouldBe([DefaultTicketTypeId]);
    }

    [TestMethod]
    public void Create_Standalone_WithEmptyTicketTypeIds_Succeeds()
    {
        var aggregate = BadgeEvent.Create(DefaultEventId, DefaultTeamId);
        var badgeTypeId = aggregate.AddBadgeType(DefaultName, BadgeKind.Standalone, []);

        var badgeType = aggregate.BadgeTypes.First(bt => bt.Id == badgeTypeId);
        badgeType.Kind.ShouldBe(BadgeKind.Standalone);
        badgeType.TicketTypeIds.ShouldBeEmpty();
    }

    [TestMethod]
    public void Create_TicketBased_WithEmptyTicketTypeIds_ThrowsBusinessRuleViolation()
    {
        var aggregate = BadgeEvent.Create(DefaultEventId, DefaultTeamId);
        Should.Throw<BusinessRuleViolationException>(() =>
            aggregate.AddBadgeType(DefaultName, BadgeKind.TicketBased, []))
            .Error.Code.ShouldBe("badges_event.ticket_type_ids_required");
    }

    // ── Rename ───────────────────────────────────────────────────────────────

    [TestMethod]
    public void Rename_ValidName_UpdatesName()
    {
        var aggregate = BadgeEvent.Create(DefaultEventId, DefaultTeamId);
        var badgeTypeId = aggregate.AddBadgeType(DefaultName, BadgeKind.Standalone, []);
        var newName = BadgeTypeName.From("Volunteer");

        aggregate.RenameBadgeType(badgeTypeId, newName);

        var badgeType = aggregate.BadgeTypes.First(bt => bt.Id == badgeTypeId);
        badgeType.Name.ShouldBe(newName);
    }
}
