using Amolenk.Admitto.Core.Badges.Domain.Entities;
using Amolenk.Admitto.Core.Badges.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Kernel.ErrorHandling;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;
using Amolenk.Admitto.Testing.Infrastructure.Assertions;
using Shouldly;

namespace Amolenk.Admitto.Core.Badges.Domain.Tests.Entities;

[TestClass]
public sealed class BadgeEventTests
{
    private static readonly TicketedEventId DefaultEventId = TicketedEventId.New();
    private static readonly TeamId DefaultTeamId = TeamId.New();
    private static readonly BadgeTypeName DefaultName = BadgeTypeName.From("Speaker");
    private static readonly TicketTypeId DefaultTicketTypeId = TicketTypeId.New();

    // ── Create ───────────────────────────────────────────────────────────────

    // When a badge event is created for a ticketed event and team
    // Then it is active with no badge types
    [TestMethod]
    public void Create_ValidInputs_CreatesActiveEventWithNoBadgeTypes()
    {
        var sut = BadgeEvent.Create(DefaultEventId, DefaultTeamId);

        sut.Id.ShouldBe(DefaultEventId);
        sut.TeamId.ShouldBe(DefaultTeamId);
        sut.Status.ShouldBe(BadgeEventStatus.Active);
        sut.BadgeTypes.ShouldBeEmpty();
    }

    // ── MarkArchived / EnsureEventActive ────────────────────────────────────

    // Given an active badge event
    // When it is archived
    // Then its status becomes archived
    [TestMethod]
    public void MarkArchived_ActiveEvent_SetsStatusToArchived()
    {
        var sut = BadgeEvent.Create(DefaultEventId, DefaultTeamId);

        sut.MarkArchived();

        sut.Status.ShouldBe(BadgeEventStatus.Archived);
    }

    // Given an active badge event
    // When the active guard is checked
    // Then it does not throw
    [TestMethod]
    public void EnsureEventActive_ActiveEvent_DoesNotThrow()
    {
        var sut = BadgeEvent.Create(DefaultEventId, DefaultTeamId);

        Should.NotThrow(() => sut.EnsureEventActive());
    }

    // Given an archived badge event
    // When the active guard is checked
    // Then it throws an event-not-active business rule violation
    [TestMethod]
    public void EnsureEventActive_ArchivedEvent_ThrowsBusinessRuleViolation()
    {
        var sut = BadgeEvent.Create(DefaultEventId, DefaultTeamId);
        sut.MarkArchived();

        Should.Throw<BusinessRuleViolationException>(() => sut.EnsureEventActive())
            .Error.ShouldMatch(BadgeEvent.Errors.EventNotActive);
    }

    // ── AddBadgeType ─────────────────────────────────────────────────────────

    // Given an event that already has a badge type named "Speaker"
    // When another badge type is added with the same name in a different case
    // Then it throws a badge-type-name-already-exists business rule violation
    [TestMethod]
    public void AddBadgeType_DuplicateNameDifferentCase_ThrowsBusinessRuleViolation()
    {
        var sut = BadgeEvent.Create(DefaultEventId, DefaultTeamId);
        sut.AddBadgeType(DefaultName, BadgeKind.Standalone, []);

        Should.Throw<BusinessRuleViolationException>(() =>
                sut.AddBadgeType(BadgeTypeName.From("SPEAKER"), BadgeKind.Standalone, []))
            .Error.ShouldMatch(BadgeEvent.Errors.BadgeTypeNameAlreadyExists);
    }

    // Given an archived badge event
    // When a badge type is added to it
    // Then it throws an event-not-active business rule violation
    [TestMethod]
    public void AddBadgeType_ArchivedEvent_ThrowsBusinessRuleViolation()
    {
        var sut = BadgeEvent.Create(DefaultEventId, DefaultTeamId);
        sut.MarkArchived();

        Should.Throw<BusinessRuleViolationException>(() =>
                sut.AddBadgeType(DefaultName, BadgeKind.Standalone, []))
            .Error.ShouldMatch(BadgeEvent.Errors.EventNotActive);
    }

    // ── RenameBadgeType ──────────────────────────────────────────────────────

    // Given an event with no badge type matching the given id
    // When a rename is attempted for that id
    // Then it throws a badge-type-not-found business rule violation
    [TestMethod]
    public void RenameBadgeType_UnknownBadgeTypeId_ThrowsBusinessRuleViolation()
    {
        var sut = BadgeEvent.Create(DefaultEventId, DefaultTeamId);

        Should.Throw<BusinessRuleViolationException>(() =>
                sut.RenameBadgeType(BadgeTypeId.New(), BadgeTypeName.From("New Name")))
            .Error.ShouldMatch(BadgeEvent.Errors.BadgeTypeNotFound);
    }

    // Given an event with two badge types, "Speaker" and "Volunteer"
    // When "Volunteer" is renamed to "Speaker" (a different case of the existing name)
    // Then it throws a badge-type-name-already-exists business rule violation
    [TestMethod]
    public void RenameBadgeType_NameCollidesWithAnotherBadgeType_ThrowsBusinessRuleViolation()
    {
        var sut = BadgeEvent.Create(DefaultEventId, DefaultTeamId);
        sut.AddBadgeType(DefaultName, BadgeKind.Standalone, []);
        var volunteerId = sut.AddBadgeType(BadgeTypeName.From("Volunteer"), BadgeKind.Standalone, []);

        Should.Throw<BusinessRuleViolationException>(() =>
                sut.RenameBadgeType(volunteerId, BadgeTypeName.From("speaker")))
            .Error.ShouldMatch(BadgeEvent.Errors.BadgeTypeNameAlreadyExists);
    }

    // Given a badge type renamed to the same name it already has
    // When the rename is applied
    // Then it succeeds because the collision check excludes the badge type itself
    [TestMethod]
    public void RenameBadgeType_SameNameAsItself_Succeeds()
    {
        var sut = BadgeEvent.Create(DefaultEventId, DefaultTeamId);
        var badgeTypeId = sut.AddBadgeType(DefaultName, BadgeKind.Standalone, []);

        Should.NotThrow(() => sut.RenameBadgeType(badgeTypeId, DefaultName));
    }

    // Given an archived badge event with a badge type
    // When the badge type is renamed
    // Then it throws an event-not-active business rule violation
    [TestMethod]
    public void RenameBadgeType_ArchivedEvent_ThrowsBusinessRuleViolation()
    {
        var sut = BadgeEvent.Create(DefaultEventId, DefaultTeamId);
        var badgeTypeId = sut.AddBadgeType(DefaultName, BadgeKind.Standalone, []);
        sut.MarkArchived();

        Should.Throw<BusinessRuleViolationException>(() =>
                sut.RenameBadgeType(badgeTypeId, BadgeTypeName.From("New Name")))
            .Error.ShouldMatch(BadgeEvent.Errors.EventNotActive);
    }

    // ── DeleteBadgeType ──────────────────────────────────────────────────────

    // Given an event with a standalone badge type
    // When the badge type is deleted
    // Then it is removed from the event's badge types and its kind is returned
    [TestMethod]
    public void DeleteBadgeType_ExistingBadgeType_RemovesItAndReturnsKind()
    {
        var sut = BadgeEvent.Create(DefaultEventId, DefaultTeamId);
        var badgeTypeId = sut.AddBadgeType(DefaultName, BadgeKind.Standalone, []);

        var kind = sut.DeleteBadgeType(badgeTypeId);

        kind.ShouldBe(BadgeKind.Standalone);
        sut.BadgeTypes.ShouldNotContain(bt => bt.Id == badgeTypeId);
    }

    // Given an event with no badge type matching the given id
    // When a delete is attempted for that id
    // Then it throws a badge-type-not-found business rule violation
    [TestMethod]
    public void DeleteBadgeType_UnknownBadgeTypeId_ThrowsBusinessRuleViolation()
    {
        var sut = BadgeEvent.Create(DefaultEventId, DefaultTeamId);

        Should.Throw<BusinessRuleViolationException>(() => sut.DeleteBadgeType(BadgeTypeId.New()))
            .Error.ShouldMatch(BadgeEvent.Errors.BadgeTypeNotFound);
    }

    // Given an archived badge event with a badge type
    // When the badge type is deleted
    // Then it throws an event-not-active business rule violation
    [TestMethod]
    public void DeleteBadgeType_ArchivedEvent_ThrowsBusinessRuleViolation()
    {
        var sut = BadgeEvent.Create(DefaultEventId, DefaultTeamId);
        var badgeTypeId = sut.AddBadgeType(DefaultName, BadgeKind.Standalone, []);
        sut.MarkArchived();

        Should.Throw<BusinessRuleViolationException>(() => sut.DeleteBadgeType(badgeTypeId))
            .Error.ShouldMatch(BadgeEvent.Errors.EventNotActive);
    }

    // ── EnsureCanManageInstances ─────────────────────────────────────────────

    // Given an active event with a standalone badge type
    // When the manage-instances guard is checked for that badge type
    // Then it does not throw
    [TestMethod]
    public void EnsureCanManageInstances_StandaloneBadgeType_DoesNotThrow()
    {
        var sut = BadgeEvent.Create(DefaultEventId, DefaultTeamId);
        var badgeTypeId = sut.AddBadgeType(DefaultName, BadgeKind.Standalone, []);

        Should.NotThrow(() => sut.EnsureCanManageInstances(badgeTypeId));
    }

    // Given an active event with a ticket-based badge type
    // When the manage-instances guard is checked for that badge type
    // Then it throws a not-standalone-badge-type business rule violation
    [TestMethod]
    public void EnsureCanManageInstances_TicketBasedBadgeType_ThrowsBusinessRuleViolation()
    {
        var sut = BadgeEvent.Create(DefaultEventId, DefaultTeamId);
        var badgeTypeId = sut.AddBadgeType(DefaultName, BadgeKind.TicketBased, [DefaultTicketTypeId]);

        Should.Throw<BusinessRuleViolationException>(() => sut.EnsureCanManageInstances(badgeTypeId))
            .Error.ShouldMatch(BadgeEvent.Errors.NotStandaloneBadgeType);
    }

    // Given an event with no badge type matching the given id
    // When the manage-instances guard is checked for that id
    // Then it throws a badge-type-not-found business rule violation
    [TestMethod]
    public void EnsureCanManageInstances_UnknownBadgeTypeId_ThrowsBusinessRuleViolation()
    {
        var sut = BadgeEvent.Create(DefaultEventId, DefaultTeamId);

        Should.Throw<BusinessRuleViolationException>(() => sut.EnsureCanManageInstances(BadgeTypeId.New()))
            .Error.ShouldMatch(BadgeEvent.Errors.BadgeTypeNotFound);
    }

    // Given an archived event with a standalone badge type
    // When the manage-instances guard is checked for that badge type
    // Then it throws an event-not-active business rule violation
    [TestMethod]
    public void EnsureCanManageInstances_ArchivedEvent_ThrowsBusinessRuleViolation()
    {
        var sut = BadgeEvent.Create(DefaultEventId, DefaultTeamId);
        var badgeTypeId = sut.AddBadgeType(DefaultName, BadgeKind.Standalone, []);
        sut.MarkArchived();

        Should.Throw<BusinessRuleViolationException>(() => sut.EnsureCanManageInstances(badgeTypeId))
            .Error.ShouldMatch(BadgeEvent.Errors.EventNotActive);
    }
}
