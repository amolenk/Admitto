using Amolenk.Admitto.Core.Badges.Domain.Entities;
using Amolenk.Admitto.Core.Badges.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;
using Shouldly;
using Vogen;

namespace Amolenk.Admitto.Core.Badges.Domain.Tests.Entities;

[TestClass]
public sealed class BadgeInstanceTests
{
    private static readonly BadgeInstanceId DefaultId = BadgeInstanceId.New();
    private static readonly TeamId DefaultTeamId = TeamId.New();
    private static readonly TicketedEventId DefaultEventId = TicketedEventId.New();
    private static readonly BadgeTypeId DefaultBadgeTypeId = BadgeTypeId.New();
    private static readonly BadgeInstanceDisplayName DefaultDisplayName =
        BadgeInstanceDisplayName.From("Jane Doe");
    private static readonly BadgeInstanceNotes DefaultNotes = BadgeInstanceNotes.From("VIP");

    // ── Create ───────────────────────────────────────────────────────────────

    // When a badge instance is created with valid inputs
    // Then it is created with the given id, team, event, badge type, display name, and notes
    [TestMethod]
    public void Create_ValidInputs_Succeeds()
    {
        var sut = BadgeInstance.Create(DefaultId, DefaultTeamId, DefaultEventId, DefaultBadgeTypeId, DefaultDisplayName, DefaultNotes);

        sut.Id.ShouldBe(DefaultId);
        sut.TeamId.ShouldBe(DefaultTeamId);
        sut.EventId.ShouldBe(DefaultEventId);
        sut.BadgeTypeId.ShouldBe(DefaultBadgeTypeId);
        sut.DisplayName.ShouldBe(DefaultDisplayName);
        sut.Notes.ShouldBe(DefaultNotes);
    }

    // When a badge instance is created with an empty display name
    // Then it throws a value object validation exception
    [TestMethod]
    public void Create_EmptyDisplayName_ThrowsValueObjectValidationException()
    {
        Should.Throw<ValueObjectValidationException>(() =>
            BadgeInstance.Create(DefaultId, DefaultTeamId, DefaultEventId, DefaultBadgeTypeId,
                BadgeInstanceDisplayName.From(""), DefaultNotes));
    }

    // When a badge instance is created with a display name longer than the maximum allowed length
    // Then it throws a value object validation exception
    [TestMethod]
    public void Create_DisplayNameExceedsMaxLength_ThrowsValueObjectValidationException()
    {
        Should.Throw<ValueObjectValidationException>(() =>
            BadgeInstance.Create(DefaultId, DefaultTeamId, DefaultEventId, DefaultBadgeTypeId,
                BadgeInstanceDisplayName.From(new string('A', BadgeInstanceDisplayName.MaxLength + 1)),
                DefaultNotes));
    }

    // When a badge instance is created with notes longer than the maximum allowed length
    // Then it throws a value object validation exception
    [TestMethod]
    public void Create_NotesExceedMaxLength_ThrowsValueObjectValidationException()
    {
        Should.Throw<ValueObjectValidationException>(() =>
            BadgeInstance.Create(DefaultId, DefaultTeamId, DefaultEventId, DefaultBadgeTypeId,
                DefaultDisplayName,
                BadgeInstanceNotes.From(new string('A', BadgeInstanceNotes.MaxLength + 1))));
    }

    // ── Update ───────────────────────────────────────────────────────────────

    // Given an existing badge instance
    // When it is updated with a new display name and notes
    // Then the display name and notes are updated
    [TestMethod]
    public void Update_ValidInputs_UpdatesDisplayNameAndNotes()
    {
        var sut = BadgeInstance.Create(DefaultId, DefaultTeamId, DefaultEventId, DefaultBadgeTypeId, DefaultDisplayName, DefaultNotes);
        var newName = BadgeInstanceDisplayName.From("John Smith");
        var newNotes = BadgeInstanceNotes.From("Updated note");

        sut.Update(newName, newNotes);

        sut.DisplayName.ShouldBe(newName);
        sut.Notes.ShouldBe(newNotes);
    }
}
