using Amolenk.Admitto.Core.Badges.Domain.Entities;
using Amolenk.Admitto.Core.Badges.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Kernel.ErrorHandling;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;
using Amolenk.Admitto.Testing.Infrastructure.Assertions;
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

    // Given a badge event
    // When a ticket-based badge type is added with a list of ticket type ids
    // Then it is created with the given name, kind, and ticket type ids
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

    // Given a badge event
    // When a standalone badge type is added with no ticket type ids
    // Then it is created successfully with an empty ticket type id list
    [TestMethod]
    public void Create_Standalone_WithEmptyTicketTypeIds_Succeeds()
    {
        var aggregate = BadgeEvent.Create(DefaultEventId, DefaultTeamId);
        var badgeTypeId = aggregate.AddBadgeType(DefaultName, BadgeKind.Standalone, []);

        var badgeType = aggregate.BadgeTypes.First(bt => bt.Id == badgeTypeId);
        badgeType.Kind.ShouldBe(BadgeKind.Standalone);
        badgeType.TicketTypeIds.ShouldBeEmpty();
    }

    // Given a badge event
    // When a ticket-based badge type is added with no ticket type ids
    // Then it throws a ticket-type-ids-required business rule violation
    [TestMethod]
    public void Create_TicketBased_WithEmptyTicketTypeIds_ThrowsBusinessRuleViolation()
    {
        var aggregate = BadgeEvent.Create(DefaultEventId, DefaultTeamId);
        Should.Throw<BusinessRuleViolationException>(() =>
            aggregate.AddBadgeType(DefaultName, BadgeKind.TicketBased, []))
            .Error.ShouldMatch(BadgeEvent.Errors.TicketTypeIdsRequired);
    }

    // ── Rename ───────────────────────────────────────────────────────────────

    // Given a badge type with an existing name
    // When it is renamed to a new valid name
    // Then its name is updated to the new value
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
