using Amolenk.Admitto.Core.Badges.Domain.Entities;
using Amolenk.Admitto.Core.Badges.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Kernel.ErrorHandling;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;
using Shouldly;

namespace Amolenk.Admitto.Core.Badges.Domain.Tests.Entities;

[TestClass]
public sealed class BadgeTypeTests
{
    private static readonly BadgeTypeId DefaultId = BadgeTypeId.New();
    private static readonly TicketedEventId DefaultEventId = TicketedEventId.New();
    private static readonly BadgeTypeName DefaultName = BadgeTypeName.From("Speaker");
    private static readonly TicketTypeId DefaultTicketTypeId = TicketTypeId.New();

    // ── Create ───────────────────────────────────────────────────────────────

    [TestMethod]
    public void Create_TicketBased_WithTicketTypeIds_Succeeds()
    {
        var sut = BadgeType.Create(DefaultId, DefaultEventId, DefaultName, BadgeKind.TicketBased,
            [DefaultTicketTypeId]);

        sut.Id.ShouldBe(DefaultId);
        sut.EventId.ShouldBe(DefaultEventId);
        sut.Name.ShouldBe(DefaultName);
        sut.Kind.ShouldBe(BadgeKind.TicketBased);
        sut.TicketTypeIds.ShouldBe([DefaultTicketTypeId]);
    }

    [TestMethod]
    public void Create_Standalone_WithEmptyTicketTypeIds_Succeeds()
    {
        var sut = BadgeType.Create(DefaultId, DefaultEventId, DefaultName, BadgeKind.Standalone, []);

        sut.Kind.ShouldBe(BadgeKind.Standalone);
        sut.TicketTypeIds.ShouldBeEmpty();
    }

    [TestMethod]
    public void Create_TicketBased_WithEmptyTicketTypeIds_ThrowsBusinessRuleViolation()
    {
        Should.Throw<BusinessRuleViolationException>(() =>
            BadgeType.Create(DefaultId, DefaultEventId, DefaultName, BadgeKind.TicketBased, []))
            .Error.Code.ShouldBe("badge_type.ticket_type_ids_required");
    }

    // ── Rename ───────────────────────────────────────────────────────────────

    [TestMethod]
    public void Rename_ValidName_UpdatesName()
    {
        var sut = BadgeType.Create(DefaultId, DefaultEventId, DefaultName, BadgeKind.Standalone, []);
        var newName = BadgeTypeName.From("Volunteer");

        sut.Rename(newName);

        sut.Name.ShouldBe(newName);
    }
}
