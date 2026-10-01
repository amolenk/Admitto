using Amolenk.Admitto.Core.Registrations.Domain.DomainEvents;
using Amolenk.Admitto.Core.Registrations.Domain.Entities;
using Amolenk.Admitto.Core.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;
using Amolenk.Admitto.Testing.Builders.Registrations.Domain;
using Amolenk.Admitto.Testing.Infrastructure.Assertions;
using Shouldly;

namespace Amolenk.Admitto.Core.Registrations.Domain.Tests.Entities;

[TestClass]
public sealed class TicketCatalogTests
{
    private static readonly TicketedEventId DefaultEventId = TicketedEventId.New();
    private static readonly TeamId DefaultTeamId = TeamId.New();

    // Given a catalog for an active event
    // When a ticket type is added
    // Then it is added with the given name, capacity, and zero used capacity
    [TestMethod]
    public void AddTicketType_ActiveEvent_AddsSuccessfully()
    {
        // Arrange
        var sut = TicketCatalog.Create(DefaultEventId, DefaultTeamId);
        var id = TicketTypeId.New();

        // Act
        sut.AddTicketType(
            id,
            TicketTypeName.From("VIP Pass"),
            [TimeSlot.From("morning")],
            100);

        // Assert
        sut.TicketTypes.Count.ShouldBe(1);
        var tt = sut.TicketTypes[0];
        tt.Id.ShouldBe(id);
        tt.Name.ShouldBe(TicketTypeName.From("VIP Pass"));
        tt.PublicCapacity.ShouldBe(100);
        tt.PublicUsedCapacity.ShouldBe(0);
    }

    // When a ticket type is added without a maximum capacity
    // Then its maximum capacity is null
    [TestMethod]
    public void AddTicketType_NoCapacity_SetsNullPublicCapacity()
    {
        // Arrange
        var sut = TicketCatalog.Create(DefaultEventId, DefaultTeamId);

        // Act
        sut.AddTicketType(
            TicketTypeId.New(),
            TicketTypeName.From("Speaker Pass"),
            [],
            publicCapacity: null);

        // Assert
        sut.TicketTypes[0].PublicCapacity.ShouldBeNull();
    }

    // Given a waitlist-enabled ticket type with a public capacity of zero
    // When the ticket type is added
    // Then it is immediately sold out to the public, activating waitlist mode and raising the event
    [TestMethod]
    public void AddTicketType_ZeroPublicCapacityWithWaitlistEnabled_ActivatesWaitlistModeImmediately()
    {
        // Arrange
        var sut = TicketCatalog.Create(DefaultEventId, DefaultTeamId);
        var id = TicketTypeId.New();

        // Act
        sut.AddTicketType(id, TicketTypeName.From("General"), [], publicCapacity: 0, waitlistEnabled: true);

        // Assert
        var tt = sut.GetTicketType(id)!;
        tt.WaitlistMode.ShouldBeTrue();
        sut.GetDomainEvents().OfType<WaitlistModeActivatedDomainEvent>()
            .ShouldHaveSingleItem()
            .TicketTypeId.ShouldBe(id);
    }

    // Given a catalog that already has a ticket type named "VIP"
    // When another ticket type is added with the same name
    // Then it throws DuplicateTicketTypeName
    [TestMethod]
    public void AddTicketType_DuplicateName_Throws()
    {
        // Arrange
        var sut = TicketCatalog.Create(DefaultEventId, DefaultTeamId);
        sut.AddTicketType(TicketTypeId.New(), TicketTypeName.From("VIP"), [], 100);

        // Act
        var result = ErrorResult.Capture(() =>
            sut.AddTicketType(TicketTypeId.New(), TicketTypeName.From("VIP"), [], 50));

        // Assert
        result.Error.ShouldMatch(TicketCatalog.Errors.DuplicateTicketTypeName(TicketTypeName.From("VIP")));
    }

    // Given an existing ticket type
    // When its maximum capacity is updated
    // Then the new capacity is applied
    [TestMethod]
    public void UpdateTicketType_Capacity_Updates()
    {
        // Arrange
        var sut = TicketCatalog.Create(DefaultEventId, DefaultTeamId);
        var id = TicketTypeId.New();
        sut.AddTicketType(id, TicketTypeName.From("VIP"), [], 100);

        // Act
        sut.UpdateTicketType(id, name: null, publicCapacity: 200);

        // Assert
        sut.TicketTypes[0].PublicCapacity.ShouldBe(200);
    }

    // Given a ticket type with 8 of 10 public seats used
    // When its public capacity is lowered to 5
    // Then availability goes negative and the ticket type is sold out to the public
    [TestMethod]
    public void UpdateTicketType_LowerPublicCapacityBelowUsed_LeavesAvailabilityNegativeAndSoldOut()
    {
        // Arrange
        var sut = TicketCatalog.Create(DefaultEventId, DefaultTeamId);
        var id = TicketTypeId.New();
        sut.AddTicketType(id, TicketTypeName.From("General"), [], publicCapacity: 10);
        for (var i = 0; i < 8; i++)
            sut.Claim([id], ClaimMode.Public);

        // Act
        sut.UpdateTicketType(id, name: null, publicCapacity: 5);

        // Assert
        var tt = sut.GetTicketType(id)!;
        tt.AvailableCapacity.ShouldBe(-3);
        tt.PublicAvailableCapacity.ShouldBe(0);
        tt.IsSoldOut.ShouldBeTrue();
        ErrorResult.Capture(() => sut.Claim([id], ClaimMode.Public))
            .Error.ShouldMatch(TicketType.Errors.TicketTypeAtCapacity(id));
    }

    // Given a ticket type whose public seats are all used
    // When an admin claims a ticket
    // Then the claim succeeds as an admin ticket and public availability is unchanged
    [TestMethod]
    public void Claim_AdminOnSoldOutTicketType_LeavesPublicAvailabilityUnchanged()
    {
        // Arrange
        var sut = TicketCatalog.Create(DefaultEventId, DefaultTeamId);
        var id = TicketTypeId.New();
        sut.AddTicketType(id, TicketTypeName.From("General"), [], publicCapacity: 2);
        sut.Claim([id], ClaimMode.Public);
        sut.Claim([id], ClaimMode.Public);

        // Act
        var tickets = sut.Claim([id], ClaimMode.Admin);

        // Assert
        var tt = sut.GetTicketType(id)!;
        tickets.ShouldHaveSingleItem().Mode.ShouldBe(ClaimMode.Admin);
        tt.AdminUsedCount.ShouldBe(1);
        tt.PublicUsedCapacity.ShouldBe(2);
        tt.AvailableCapacity.ShouldBe(0);
    }

    // Given admins claim tickets before any public sales
    // When public claims fill the public capacity
    // Then the full public capacity remains sellable — admin tickets come on top of it
    [TestMethod]
    public void Claim_AdminBeforePublicClaims_DoesNotReducePublicAvailability()
    {
        // Arrange
        var sut = TicketCatalog.Create(DefaultEventId, DefaultTeamId);
        var id = TicketTypeId.New();
        sut.AddTicketType(id, TicketTypeName.From("General"), [], publicCapacity: 200);
        for (var i = 0; i < 10; i++)
            sut.Claim([id], ClaimMode.Admin);
        sut.GetTicketType(id)!.AvailableCapacity.ShouldBe(200);

        // Act
        for (var i = 0; i < 200; i++)
            sut.Claim([id], ClaimMode.Public);

        // Assert
        var tt = sut.GetTicketType(id)!;
        tt.IsSoldOut.ShouldBeTrue();
        tt.PublicUsedCapacity.ShouldBe(200);
        tt.AdminUsedCount.ShouldBe(10);
    }

    // Given a sold-out ticket type in waitlist mode with an admin ticket
    // When the admin ticket is released
    // Then it decrements the admin count, frees no public seat and raises no WaitlistCapacityAvailable event
    [TestMethod]
    public void Release_AdminTicket_FreesNoPublicSeat()
    {
        // Arrange
        var sut = TicketCatalog.Create(DefaultEventId, DefaultTeamId);
        var id = TicketTypeId.New();
        sut.AddTicketType(id, TicketTypeName.From("General"), [], publicCapacity: 1, waitlistEnabled: true);
        sut.Claim([id], ClaimMode.Public);
        var adminTickets = sut.Claim([id], ClaimMode.Admin);
        sut.ClearDomainEvents();

        // Act
        sut.Release(adminTickets);

        // Assert
        var tt = sut.GetTicketType(id)!;
        tt.AdminUsedCount.ShouldBe(0);
        tt.PublicUsedCapacity.ShouldBe(1);
        tt.AvailableCapacity.ShouldBe(0);
        sut.GetDomainEvents().OfType<WaitlistCapacityAvailableDomainEvent>().ShouldBeEmpty();
    }

    // Given a sold-out ticket type in waitlist mode
    // When a public ticket is released
    // Then a public seat is freed and a WaitlistCapacityAvailable event is raised
    [TestMethod]
    public void Release_PublicTicketInWaitlistMode_FreesPublicSeat()
    {
        // Arrange
        var sut = TicketCatalog.Create(DefaultEventId, DefaultTeamId);
        var id = TicketTypeId.New();
        sut.AddTicketType(id, TicketTypeName.From("General"), [], publicCapacity: 1, waitlistEnabled: true);
        var publicTickets = sut.Claim([id], ClaimMode.Public);
        sut.Claim([id], ClaimMode.Admin);
        sut.ClearDomainEvents();

        // Act
        sut.Release(publicTickets);

        // Assert
        var tt = sut.GetTicketType(id)!;
        tt.PublicUsedCapacity.ShouldBe(0);
        tt.AdminUsedCount.ShouldBe(1);
        sut.GetDomainEvents().OfType<WaitlistCapacityAvailableDomainEvent>()
            .ShouldHaveSingleItem().AvailableCapacity.ShouldBe(1);
    }

    // Given an existing ticket type
    // When its name is updated
    // Then the new name is applied
    [TestMethod]
    public void UpdateTicketType_Name_Updates()
    {
        // Arrange
        var sut = TicketCatalog.Create(DefaultEventId, DefaultTeamId);
        var id = TicketTypeId.New();
        sut.AddTicketType(id, TicketTypeName.From("VIP"), [], 100);

        // Act
        sut.UpdateTicketType(id, name: TicketTypeName.From("VIP Access"), publicCapacity: 100);

        // Assert
        sut.TicketTypes[0].Name.ShouldBe(TicketTypeName.From("VIP Access"));
    }

    // Given a catalog with no matching ticket type
    // When an update is attempted for an unknown ticket type id
    // Then it throws TicketTypeNotFound
    [TestMethod]
    public void UpdateTicketType_NotFound_Throws()
    {
        // Arrange
        var sut = TicketCatalog.Create(DefaultEventId, DefaultTeamId);
        var unknownId = TicketTypeId.New();

        // Act
        var result = ErrorResult.Capture(() =>
            sut.UpdateTicketType(unknownId, name: null, publicCapacity: 100));

        // Assert
        result.Error.ShouldMatch(TicketCatalog.Errors.TicketTypeNotFound(unknownId));
    }

    // Given a ticket type with available capacity
    // When a ticket is claimed with enforcement enabled
    // Then the used capacity is incremented
    [TestMethod]
    public void Claim_Enforce_AvailableCapacity_Increments()
    {
        // Arrange
        var sut = TicketCatalog.Create(DefaultEventId, DefaultTeamId);
        var id = TicketTypeId.New();
        sut.AddTicketType(id, TicketTypeName.From("General"), [], 10);

        // Act
        sut.Claim([id], ClaimMode.Public);

        // Assert
        sut.TicketTypes[0].PublicUsedCapacity.ShouldBe(1);
    }

    // Given a ticket type already at its capacity limit
    // When a further ticket is claimed with enforcement enabled
    // Then it throws TicketTypeAtCapacity
    [TestMethod]
    public void Claim_Enforce_AtCapacity_Throws()
    {
        // Arrange
        var sut = TicketCatalog.Create(DefaultEventId, DefaultTeamId);
        var id = TicketTypeId.New();
        sut.AddTicketType(id, TicketTypeName.From("General"), [], 1);
        sut.Claim([id], ClaimMode.Public);

        // Act
        var result = ErrorResult.Capture(() => sut.Claim([id], ClaimMode.Public));

        // Assert
        result.Error.ShouldMatch(Registrations.Domain.Entities.TicketType.Errors.TicketTypeAtCapacity(id));
    }

    // Given a ticket type with no capacity limit and self-service enabled
    // When a ticket is claimed with enforcement enabled
    // Then the claim succeeds and used capacity is incremented
    [TestMethod]
    public void Claim_Enforce_NullCapacity_SelfServiceEnabled_Succeeds()
    {
        // Arrange — null capacity + self-service enabled means unlimited self-service
        var sut = TicketCatalog.Create(DefaultEventId, DefaultTeamId);
        var id = TicketTypeId.New();
        sut.AddTicketType(id, TicketTypeName.From("Speaker"), [], null, selfServiceEnabled: true);

        // Act
        sut.Claim([id], ClaimMode.Public);

        // Assert
        sut.TicketTypes[0].PublicUsedCapacity.ShouldBe(1);
    }

    // Given a ticket type already at public capacity
    // When an admin claims a ticket
    // Then the claim succeeds and counts as an admin ticket on top of public capacity
    [TestMethod]
    public void Claim_Admin_AlwaysIncrements()
    {
        // Arrange
        var sut = TicketCatalog.Create(DefaultEventId, DefaultTeamId);
        var id = TicketTypeId.New();
        sut.AddTicketType(id, TicketTypeName.From("VIP"), [], 1);
        sut.Claim([id], ClaimMode.Public); // at capacity

        // Act
        sut.Claim([id], ClaimMode.Admin); // should still work

        // Assert
        sut.TicketTypes[0].PublicUsedCapacity.ShouldBe(1);
        sut.TicketTypes[0].AdminUsedCount.ShouldBe(1);
    }

    // Given two ticket types with available capacity
    // When both are claimed together
    // Then each ticket type's used capacity is incremented
    [TestMethod]
    public void Claim_MultipleIds_AllIncrement()
    {
        // Arrange
        var sut = TicketCatalog.Create(DefaultEventId, DefaultTeamId);
        var idA = TicketTypeId.New();
        var idB = TicketTypeId.New();
        sut.AddTicketType(idA, TicketTypeName.From("A"), [], 10);
        sut.AddTicketType(idB, TicketTypeName.From("B"), [], 10);

        // Act
        sut.Claim([idA, idB], ClaimMode.Public);

        // Assert
        sut.TicketTypes.Single(t => t.Id == idA).PublicUsedCapacity.ShouldBe(1);
        sut.TicketTypes.Single(t => t.Id == idB).PublicUsedCapacity.ShouldBe(1);
    }

    // Given a catalog that does not contain a given ticket type id
    // When a claim includes that unknown id
    // Then it throws UnknownTicketTypes
    [TestMethod]
    public void Claim_UnknownId_Throws()
    {
        // Arrange
        var sut = TicketCatalog.Create(DefaultEventId, DefaultTeamId);
        var knownId = TicketTypeId.New();
        var unknownId = TicketTypeId.New();
        sut.AddTicketType(knownId, TicketTypeName.From("Known"), [], 10);

        // Act
        var result = ErrorResult.Capture(() => sut.Claim([unknownId], ClaimMode.Public));

        // Assert
        result.Error.ShouldMatch(TicketCatalog.Errors.UnknownTicketTypes([unknownId.Value]));
    }

    // Given a ticket type exists in the catalog
    // When it is looked up by id
    // Then the matching ticket type is returned
    [TestMethod]
    public void GetTicketType_Exists_Returns()
    {
        // Arrange
        var sut = TicketCatalog.Create(DefaultEventId, DefaultTeamId);
        var id = TicketTypeId.New();
        sut.AddTicketType(id, TicketTypeName.From("VIP"), [], 100);

        // Act
        var tt = sut.GetTicketType(id);

        // Assert
        tt.ShouldNotBeNull();
        tt.Id.ShouldBe(id);
    }

    // Given a catalog with no matching ticket type
    // When it is looked up by an unknown id
    // Then null is returned
    [TestMethod]
    public void GetTicketType_NotExists_ReturnsNull()
    {
        // Arrange
        var sut = TicketCatalog.Create(DefaultEventId, DefaultTeamId);
        var unknownId = TicketTypeId.New();

        // Act
        var tt = sut.GetTicketType(unknownId);

        // Assert
        tt.ShouldBeNull();
    }

    // When a new catalog is created
    // Then its event status is Active
    [TestMethod]
    public void NewCatalog_EventStatusIsActive()
    {
        // Arrange
        var sut = TicketCatalog.Create(DefaultEventId, DefaultTeamId);

        // Assert
        sut.EventStatus.ShouldBe(EventLifecycleStatus.Active);
    }

    // Given a catalog for an active event
    // When the event is marked archived
    // Then the catalog's event status becomes Archived
    [TestMethod]
    public void MarkEventArchived_FromActive_Transitions()
    {
        // Arrange
        var sut = TicketCatalog.Create(DefaultEventId, DefaultTeamId);

        // Act
        sut.MarkEventArchived();

        // Assert
        sut.EventStatus.ShouldBe(EventLifecycleStatus.Archived);
    }

    // Given a catalog whose event is already archived
    // When the event is marked archived again
    // Then the event status remains Archived without error
    [TestMethod]
    public void MarkEventArchived_AlreadyArchived_IsIdempotent()
    {
        // Arrange
        var sut = TicketCatalog.Create(DefaultEventId, DefaultTeamId);
        sut.MarkEventArchived();

        // Act
        sut.MarkEventArchived();

        // Assert
        sut.EventStatus.ShouldBe(EventLifecycleStatus.Archived);
    }

    // Given the event has been archived
    // When a ticket is claimed
    // Then it throws EventNotActive
    [TestMethod]
    public void Claim_EventArchived_Throws()
    {
        // Arrange
        var sut = TicketCatalog.Create(DefaultEventId, DefaultTeamId);
        var id = TicketTypeId.New();
        sut.AddTicketType(id, TicketTypeName.From("General"), [], 10);
        sut.MarkEventArchived();

        // Act
        var result = ErrorResult.Capture(() => sut.Claim([id], ClaimMode.Admin));

        // Assert
        result.Error.ShouldMatch(TicketCatalog.Errors.EventNotActive);
    }

    // Given the event has been archived
    // When a new ticket type is added
    // Then it throws EventNotActive
    [TestMethod]
    public void AddTicketType_EventArchived_Throws()
    {
        // Arrange
        var sut = TicketCatalog.Create(DefaultEventId, DefaultTeamId);
        sut.MarkEventArchived();

        // Act
        var result = ErrorResult.Capture(() =>
            sut.AddTicketType(TicketTypeId.New(), TicketTypeName.From("VIP"), [], 100));

        // Assert
        result.Error.ShouldMatch(TicketCatalog.Errors.EventNotActive);
    }

    // Given a ticket type with a claimed ticket
    // When that ticket type id is released
    // Then its used capacity is decremented
    [TestMethod]
    public void Release_MatchingIds_DecrementsUsedCapacity()
    {
        // Arrange
        var sut = TicketCatalog.Create(DefaultEventId, DefaultTeamId);
        var id = TicketTypeId.New();
        sut.AddTicketType(id, TicketTypeName.From("General"), [], 10);
        var tickets = sut.Claim([id], ClaimMode.Public);
        sut.GetTicketType(id)!.PublicUsedCapacity.ShouldBe(1);

        // Act
        sut.Release(tickets);

        // Assert
        sut.GetTicketType(id)!.PublicUsedCapacity.ShouldBe(0);
    }

    // Given one known ticket type with a claimed ticket and one unknown ticket type id
    // When both ids are released together
    // Then the known ticket type is released and the unknown id is skipped without error
    [TestMethod]
    public void Release_UnknownId_IsSilentlySkipped()
    {
        // Arrange
        var sut = TicketCatalog.Create(DefaultEventId, DefaultTeamId);
        var knownId = TicketTypeId.New();
        var unknownId = TicketTypeId.New();
        sut.AddTicketType(knownId, TicketTypeName.From("Known"), [], 10);
        var tickets = sut.Claim([knownId], ClaimMode.Public);
        var unknownTicket = new TicketTypeSnapshot(unknownId, TicketTypeName.From("Unknown"), []);

        // Act — releasing an unknown ID should not throw
        sut.Release([unknownTicket, .. tickets]);

        // Assert — known was released; unknown was skipped without error
        sut.GetTicketType(knownId)!.PublicUsedCapacity.ShouldBe(0);
    }

    // Given two ticket types each with a claimed ticket
    // When both are released together
    // Then each ticket type's used capacity is decremented
    [TestMethod]
    public void Release_MultipleIds_AllDecrement()
    {
        // Arrange
        var sut = TicketCatalog.Create(DefaultEventId, DefaultTeamId);
        var idA = TicketTypeId.New();
        var idB = TicketTypeId.New();
        sut.AddTicketType(idA, TicketTypeName.From("A"), [], 10);
        sut.AddTicketType(idB, TicketTypeName.From("B"), [], 10);
        var tickets = sut.Claim([idA, idB], ClaimMode.Public);

        // Act
        sut.Release(tickets);

        // Assert
        sut.GetTicketType(idA)!.PublicUsedCapacity.ShouldBe(0);
        sut.GetTicketType(idB)!.PublicUsedCapacity.ShouldBe(0);
    }

    // Given a ticket type with available capacity
    // When the same ticket type id is claimed twice in one request
    // Then it throws DuplicateTicketTypes
    [TestMethod]
    public void Claim_DuplicateIds_Throws()
    {
        // Arrange
        var sut = TicketCatalog.Create(DefaultEventId, DefaultTeamId);
        var id = TicketTypeId.New();
        sut.AddTicketType(id, TicketTypeName.From("General"), [], 10);

        // Act
        var result = ErrorResult.Capture(() => sut.Claim([id, id], ClaimMode.Admin));

        // Assert
        result.Error.ShouldMatch(TicketCatalog.Errors.DuplicateTicketTypes([id.Value]));
    }

    // Given two ticket types that share the same time slot
    // When both are claimed together
    // Then it throws OverlappingTimeSlots
    [TestMethod]
    public void Claim_OverlappingTimeSlots_Throws()
    {
        // Arrange
        var sut = TicketCatalog.Create(DefaultEventId, DefaultTeamId);
        var idA = TicketTypeId.New();
        var idB = TicketTypeId.New();
        sut.AddTicketType(idA, TicketTypeName.From("Workshop A"),
            [TimeSlot.From("morning")], 10);
        sut.AddTicketType(idB, TicketTypeName.From("Workshop B"),
            [TimeSlot.From("morning")], 10);

        // Act
        var result = ErrorResult.Capture(() => sut.Claim([idA, idB], ClaimMode.Admin));

        // Assert
        result.Error.ShouldMatch(TicketCatalog.Errors.OverlappingTimeSlots(["morning"]));
    }

    // Given a ticket type with available capacity
    // When a claim is made with an empty list of ticket type ids
    // Then nothing happens and used capacity is unchanged
    [TestMethod]
    public void Claim_EmptyList_IsNoOp()
    {
        // Arrange
        var sut = TicketCatalog.Create(DefaultEventId, DefaultTeamId);
        var id = TicketTypeId.New();
        sut.AddTicketType(id, TicketTypeName.From("General"), [], 10);

        // Act — empty claim should not throw
        sut.Claim([], ClaimMode.Public);

        // Assert — capacity unchanged
        sut.GetTicketType(id)!.PublicUsedCapacity.ShouldBe(0);
    }

    // Given a ticket type that does not allow self-service
    // When it is claimed with enforcement enabled
    // Then it throws TicketTypesNotSelfService
    [TestMethod]
    public void Claim_Enforce_NonSelfServiceTicketType_Throws()
    {
        // Arrange
        var sut = TicketCatalog.Create(DefaultEventId, DefaultTeamId);
        var id = TicketTypeId.New();
        sut.AddTicketType(id, TicketTypeName.From("VIP"), [], 50, selfServiceEnabled: false);

        // Act
        var result = ErrorResult.Capture(() => sut.Claim([id], ClaimMode.Public));

        // Assert
        result.Error.ShouldMatch(TicketCatalog.Errors.TicketTypesNotSelfService([id.Value]));
    }

    // Given a ticket type that does not allow self-service
    // When it is claimed without enforcement, such as an admin or coupon claim
    // Then the claim succeeds and used capacity is incremented
    [TestMethod]
    public void Claim_NoEnforce_NonSelfServiceTicketType_Succeeds()
    {
        // Arrange — admin/coupon bypass: enforce=false skips self-service check
        var sut = TicketCatalog.Create(DefaultEventId, DefaultTeamId);
        var id = TicketTypeId.New();
        sut.AddTicketType(id, TicketTypeName.From("VIP"), [], 50, selfServiceEnabled: false);

        // Act
        sut.Claim([id], ClaimMode.Admin);

        // Assert
        sut.GetTicketType(id)!.AdminUsedCount.ShouldBe(1);
    }

    // ── Waitlist ─────────────────────────────────────────────────────────────

    // Given a ticket type with waitlist enabled and one slot remaining
    // When the last slot is claimed with enforcement enabled
    // Then waitlist mode is activated and a WaitlistModeActivated event is raised
    [TestMethod]
    public void Claim_Enforce_LastSlotWithWaitlistEnabled_ActivatesWaitlistMode()
    {
        // Arrange — capacity of 2, sell first slot, then the second (last) slot
        var sut = TicketCatalog.Create(DefaultEventId, DefaultTeamId);
        var id = TicketTypeId.New();
        sut.AddTicketType(id, TicketTypeName.From("General"), [], 2, waitlistEnabled: true);
        sut.Claim([id], ClaimMode.Public);

        // Act — claim the last slot
        sut.Claim([id], ClaimMode.Public);

        // Assert
        var tt = sut.GetTicketType(id)!;
        tt.WaitlistMode.ShouldBeTrue();
        sut.GetDomainEvents().OfType<WaitlistModeActivatedDomainEvent>()
            .ShouldHaveSingleItem()
            .TicketTypeId.ShouldBe(id);
    }

    // Given a ticket type with waitlist disabled and one slot remaining
    // When the last slot is claimed with enforcement enabled
    // Then waitlist mode is not activated and no WaitlistModeActivated event is raised
    [TestMethod]
    public void Claim_Enforce_LastSlotWithWaitlistDisabled_DoesNotActivateWaitlistMode()
    {
        // Arrange
        var sut = TicketCatalog.Create(DefaultEventId, DefaultTeamId);
        var id = TicketTypeId.New();
        sut.AddTicketType(id, TicketTypeName.From("General"), [], 1, waitlistEnabled: false);

        // Act — claim the only slot
        sut.Claim([id], ClaimMode.Public);

        // Assert
        sut.GetTicketType(id)!.WaitlistMode.ShouldBeFalse();
        sut.GetDomainEvents().OfType<WaitlistModeActivatedDomainEvent>().ShouldBeEmpty();
    }

    // Given a ticket type with waitlist enabled and one slot remaining
    // When the last slot is claimed without enforcement, such as an admin or coupon claim
    // Then waitlist mode is not activated and no WaitlistModeActivated event is raised
    [TestMethod]
    public void Claim_NoEnforce_LastSlotWithWaitlistEnabled_DoesNotActivateWaitlistMode()
    {
        // Admin/coupon path bypasses waitlist mode activation
        var sut = TicketCatalog.Create(DefaultEventId, DefaultTeamId);
        var id = TicketTypeId.New();
        sut.AddTicketType(id, TicketTypeName.From("General"), [], 1, waitlistEnabled: true);

        // Act
        sut.Claim([id], ClaimMode.Admin);

        // Assert
        sut.GetTicketType(id)!.WaitlistMode.ShouldBeFalse();
        sut.GetDomainEvents().OfType<WaitlistModeActivatedDomainEvent>().ShouldBeEmpty();
    }

    // Given a ticket type that is fully sold out
    // When waitlist is enabled for it via an update
    // Then waitlist mode activates immediately and a WaitlistModeActivated event is raised
    [TestMethod]
    public void UpdateTicketType_EnableWaitlistOnSoldOutType_ActivatesWaitlistModeImmediately()
    {
        // Arrange — fully sold out before enabling waitlist
        var sut = TicketCatalog.Create(DefaultEventId, DefaultTeamId);
        var id = TicketTypeId.New();
        sut.AddTicketType(id, TicketTypeName.From("General"), [], 2);
        sut.Claim([id], ClaimMode.Public);
        sut.Claim([id], ClaimMode.Public);

        // Act
        sut.UpdateTicketType(id, name: null, publicCapacity: 2, waitlistEnabled: true);

        // Assert
        var tt = sut.GetTicketType(id)!;
        tt.WaitlistEnabled.ShouldBeTrue();
        tt.WaitlistMode.ShouldBeTrue();
        sut.GetDomainEvents().OfType<WaitlistModeActivatedDomainEvent>()
            .ShouldHaveSingleItem()
            .TicketTypeId.ShouldBe(id);
    }

    // Given a ticket type with one slot still available
    // When waitlist is enabled for it via an update
    // Then waitlist mode does not activate and no WaitlistModeActivated event is raised
    [TestMethod]
    public void UpdateTicketType_EnableWaitlistOnPartiallyFilledType_DoesNotActivateWaitlistMode()
    {
        // Arrange — one slot still available
        var sut = TicketCatalog.Create(DefaultEventId, DefaultTeamId);
        var id = TicketTypeId.New();
        sut.AddTicketType(id, TicketTypeName.From("General"), [], 2);
        sut.Claim([id], ClaimMode.Public);

        // Act
        sut.UpdateTicketType(id, name: null, publicCapacity: 2, waitlistEnabled: true);

        // Assert
        sut.GetTicketType(id)!.WaitlistMode.ShouldBeFalse();
        sut.GetDomainEvents().OfType<WaitlistModeActivatedDomainEvent>().ShouldBeEmpty();
    }

    // Given a ticket type currently in waitlist mode
    // When waitlist is disabled for it via an update
    // Then waitlist mode is switched off and a WaitlistDisabled event is raised with no freed slots
    [TestMethod]
    public void UpdateTicketType_DisableWaitlistWhileInWaitlistMode_RaisesWaitlistDisabledEvent()
    {
        // Arrange — waitlist mode active
        var sut = TicketCatalog.Create(DefaultEventId, DefaultTeamId);
        var id = TicketTypeId.New();
        sut.AddTicketType(id, TicketTypeName.From("General"), [], 1, waitlistEnabled: true);
        sut.Claim([id], ClaimMode.Public); // fills capacity → WaitlistMode activates

        // Act
        sut.UpdateTicketType(id, name: null, publicCapacity: 1, waitlistEnabled: false);

        // Assert
        var tt = sut.GetTicketType(id)!;
        tt.WaitlistEnabled.ShouldBeFalse();
        tt.WaitlistMode.ShouldBeFalse();
        var evt = sut.GetDomainEvents().OfType<WaitlistDisabledDomainEvent>().ShouldHaveSingleItem();
        evt.TicketTypeId.ShouldBe(id);
        evt.FreedSlots.ShouldBe(0);
        sut.GetDomainEvents().OfType<WaitlistCapacityLimitRemovedDomainEvent>().ShouldBeEmpty();
    }

    // Given a sold-out ticket type in waitlist mode
    // When its capacity is raised and its waitlist disabled in the same update
    // Then a single WaitlistDisabled event carries the freed slots, so they are offered before the queue is cleared
    [TestMethod]
    public void UpdateTicketType_CapacityIncreaseAndDisableWaitlist_RaisesWaitlistDisabledEventWithFreedSlots()
    {
        // Arrange — sold out and in WaitlistMode
        var sut = TicketCatalog.Create(DefaultEventId, DefaultTeamId);
        var id = TicketTypeId.New();
        sut.AddTicketType(id, TicketTypeName.From("General"), [], 1, waitlistEnabled: true);
        sut.Claim([id], ClaimMode.Public);
        sut.ClearDomainEvents();

        // Act — add 2 slots and disable the waitlist
        sut.UpdateTicketType(id, name: null, publicCapacity: 3, waitlistEnabled: false);

        // Assert
        var tt = sut.GetTicketType(id)!;
        tt.PublicCapacity.ShouldBe(3);
        tt.WaitlistEnabled.ShouldBeFalse();
        tt.WaitlistMode.ShouldBeFalse();
        sut.GetDomainEvents().OfType<WaitlistDisabledDomainEvent>()
            .ShouldHaveSingleItem()
            .FreedSlots.ShouldBe(2);
        sut.GetDomainEvents().OfType<WaitlistCapacityAvailableDomainEvent>().ShouldBeEmpty();
    }

    // Given a ticket type with waitlist enabled but not in waitlist mode
    // When its capacity is raised and its waitlist disabled in the same update
    // Then the WaitlistDisabled event reports no freed slots because nobody was waiting for them
    [TestMethod]
    public void UpdateTicketType_CapacityIncreaseAndDisableWaitlistOutsideWaitlistMode_ReportsNoFreedSlots()
    {
        // Arrange
        var sut = TicketCatalog.Create(DefaultEventId, DefaultTeamId);
        var id = TicketTypeId.New();
        sut.AddTicketType(id, TicketTypeName.From("General"), [], 5, waitlistEnabled: true);

        // Act
        sut.UpdateTicketType(id, name: null, publicCapacity: 10, waitlistEnabled: false);

        // Assert
        sut.GetDomainEvents().OfType<WaitlistDisabledDomainEvent>()
            .ShouldHaveSingleItem()
            .FreedSlots.ShouldBe(0);
    }

    // Given a ticket type with waitlist enabled and a bounded capacity
    // When the capacity limit is removed via an update
    // Then the waitlist is switched off and a WaitlistCapacityLimitRemoved event is raised instead of WaitlistDisabled
    [TestMethod]
    public void UpdateTicketType_RemoveCapacityLimitWithWaitlistEnabled_RaisesWaitlistCapacityLimitRemovedEvent()
    {
        // Arrange
        var sut = TicketCatalog.Create(DefaultEventId, DefaultTeamId);
        var id = TicketTypeId.New();
        sut.AddTicketType(id, TicketTypeName.From("General"), [], 10, waitlistEnabled: true);

        // Act — remove capacity limit (set null)
        sut.UpdateTicketType(id, name: null, publicCapacity: null);

        // Assert
        var tt = sut.GetTicketType(id)!;
        tt.WaitlistEnabled.ShouldBeFalse();
        tt.WaitlistMode.ShouldBeFalse();
        tt.PublicCapacity.ShouldBeNull();
        sut.GetDomainEvents().OfType<WaitlistCapacityLimitRemovedDomainEvent>()
            .ShouldHaveSingleItem()
            .TicketTypeId.ShouldBe(id);
        sut.GetDomainEvents().OfType<WaitlistDisabledDomainEvent>().ShouldBeEmpty();
    }

    // Given a sold-out ticket type in waitlist mode
    // When the capacity limit is removed and the waitlist switched off in the same update (as the Admin UI sends it)
    // Then it is treated as a capacity-limit removal, not an explicit disable
    [TestMethod]
    public void UpdateTicketType_RemoveCapacityLimitAndDisableWaitlist_RaisesOnlyWaitlistCapacityLimitRemovedEvent()
    {
        // Arrange
        var sut = TicketCatalog.Create(DefaultEventId, DefaultTeamId);
        var id = TicketTypeId.New();
        sut.AddTicketType(id, TicketTypeName.From("General"), [], 1, waitlistEnabled: true);
        sut.Claim([id], ClaimMode.Public);

        // Act
        sut.UpdateTicketType(id, name: null, publicCapacity: null, waitlistEnabled: false);

        // Assert
        var tt = sut.GetTicketType(id)!;
        tt.WaitlistEnabled.ShouldBeFalse();
        tt.WaitlistMode.ShouldBeFalse();
        sut.GetDomainEvents().OfType<WaitlistCapacityLimitRemovedDomainEvent>().ShouldHaveSingleItem();
        sut.GetDomainEvents().OfType<WaitlistDisabledDomainEvent>().ShouldBeEmpty();
    }

    // Given a ticket type that is sold out and in waitlist mode
    // When its capacity is increased via an update
    // Then a WaitlistCapacityAvailable event is raised reporting the number of available seats
    [TestMethod]
    public void UpdateTicketType_CapacityIncreaseWhileInWaitlistMode_RaisesWaitlistCapacityAvailableEvent()
    {
        // Arrange — sold out and in WaitlistMode
        var sut = TicketCatalog.Create(DefaultEventId, DefaultTeamId);
        var id = TicketTypeId.New();
        sut.AddTicketType(id, TicketTypeName.From("General"), [], 1, waitlistEnabled: true);
        sut.Claim([id], ClaimMode.Public); // fills to capacity → WaitlistMode on
        sut.ClearDomainEvents();

        // Act — add 3 more slots
        sut.UpdateTicketType(id, name: null, publicCapacity: 4);

        // Assert — 3 available seats
        var evt = sut.GetDomainEvents().OfType<WaitlistCapacityAvailableDomainEvent>()
            .ShouldHaveSingleItem();
        evt.TicketTypeId.ShouldBe(id);
        evt.AvailableCapacity.ShouldBe(3);
    }

    // When a ticket type is added with waitlist enabled but no maximum capacity
    // Then it throws WaitlistRequiresBoundedCapacity
    [TestMethod]
    public void AddTicketType_WaitlistEnabledWithoutCapacity_Throws()
    {
        // Arrange
        var sut = TicketCatalog.Create(DefaultEventId, DefaultTeamId);
        var id = TicketTypeId.New();

        // Act
        var result = ErrorResult.Capture(() =>
            sut.AddTicketType(id, TicketTypeName.From("General"), [], publicCapacity: null, waitlistEnabled: true));

        // Assert
        result.Error.ShouldMatch(TicketCatalog.Errors.WaitlistRequiresBoundedCapacity(id));
    }

    // Given a sold-out ticket type in waitlist mode that also has an admin ticket
    // When the admin ticket is released and waitlist mode is re-evaluated with nobody queued
    // Then waitlist mode remains active because no public seat was freed
    [TestMethod]
    public void ReEvaluateWaitlistMode_AdminTicketReleasedWhileSoldOut_KeepsWaitlistMode()
    {
        // Arrange
        var sut = TicketCatalog.Create(DefaultEventId, DefaultTeamId);
        var id = TicketTypeId.New();
        SellOut(sut, id, capacity: 1);
        var adminTickets = sut.Claim([id], ClaimMode.Admin);
        sut.Release(adminTickets);

        // Act — nobody queued, no outstanding offers
        sut.ReEvaluateWaitlistMode(id);

        // Assert
        sut.GetTicketType(id)!.WaitlistMode.ShouldBeTrue();
    }

    // Given a ticket type in waitlist mode with a slot now freed
    // When waitlist mode is re-evaluated with nobody queued and no outstanding offers
    // Then waitlist mode is cleared
    [TestMethod]
    public void ReEvaluateWaitlistMode_AllConditionsMet_ClearsWaitlistMode()
    {
        // Arrange — in WaitlistMode but capacity is now available
        var sut = TicketCatalog.Create(DefaultEventId, DefaultTeamId);
        var id = TicketTypeId.New();
        sut.AddTicketType(id, TicketTypeName.From("General"), [], 2, waitlistEnabled: true);
        sut.Claim([id], ClaimMode.Public);
        var tickets = sut.Claim([id], ClaimMode.Public); // WaitlistMode on
        sut.Release(tickets); // one slot freed

        // Act — nobody queued, no outstanding offers
        sut.ReEvaluateWaitlistMode(id);

        // Assert
        sut.GetTicketType(id)!.WaitlistMode.ShouldBeFalse();
    }

    // Given a ticket type in waitlist mode that is still at capacity
    // When waitlist mode is re-evaluated with nobody queued and no outstanding offers
    // Then waitlist mode remains active
    [TestMethod]
    public void ReEvaluateWaitlistMode_StillAtCapacity_KeepsWaitlistMode()
    {
        // Arrange — WaitlistMode on, at capacity
        var sut = TicketCatalog.Create(DefaultEventId, DefaultTeamId);
        var id = TicketTypeId.New();
        sut.AddTicketType(id, TicketTypeName.From("General"), [], 1, waitlistEnabled: true);
        sut.Claim([id], ClaimMode.Public); // WaitlistMode on

        // Act
        sut.ReEvaluateWaitlistMode(id);

        // Assert — still at capacity → stays in WaitlistMode
        sut.GetTicketType(id)!.WaitlistMode.ShouldBeTrue();
    }

    // Given a ticket type in waitlist mode with a slot freed but an attendee still queued
    // When waitlist mode is re-evaluated
    // Then waitlist mode remains active
    [TestMethod]
    public void ReEvaluateWaitlistMode_ActiveEntriesRemaining_KeepsWaitlistMode()
    {
        // Arrange — capacity freed but entries still in queue
        var sut = TicketCatalog.Create(DefaultEventId, DefaultTeamId);
        var id = TicketTypeId.New();
        sut.AddTicketType(id, TicketTypeName.From("General"), [], 2, waitlistEnabled: true);
        sut.Claim([id], ClaimMode.Public);
        var tickets = sut.Claim([id], ClaimMode.Public); // WaitlistMode on
        sut.Release(tickets); // one slot freed
        sut.JoinWaitlistQueue(id);

        // Act — an attendee is still queued
        sut.ReEvaluateWaitlistMode(id);

        // Assert
        sut.GetTicketType(id)!.WaitlistMode.ShouldBeTrue();
    }

    // Given a ticket type in waitlist mode with a seat free but a waitlist offer still outstanding
    // When waitlist mode is re-evaluated
    // Then waitlist mode remains active
    [TestMethod]
    public void ReEvaluateWaitlistMode_WaitlistOfferOutstanding_KeepsWaitlistMode()
    {
        // Arrange — sold out at 3 seats, a ticket is released and held by an automatic offer, then another released
        var sut = TicketCatalog.Create(DefaultEventId, DefaultTeamId);
        var id = TicketTypeId.New();
        sut.AddTicketType(id, TicketTypeName.From("General"), [], 3, waitlistEnabled: true);
        var first = sut.Claim([id], ClaimMode.Public);
        var second = sut.Claim([id], ClaimMode.Public);
        sut.Claim([id], ClaimMode.Public); // WaitlistMode on
        sut.Release(first);
        sut.HoldForWaitlistOffer(id);
        sut.Release(second); // one seat free besides the one the offer holds

        // Act — the offer is still in flight
        sut.ReEvaluateWaitlistMode(id);

        // Assert
        sut.GetTicketType(id)!.WaitlistMode.ShouldBeTrue();
    }

    // Given a sold-out ticket type in waitlist mode with nobody queued and no outstanding offers
    // When waitlist mode is lifted because the waitlist is exhausted
    // Then waitlist mode is cleared even though it is sold out
    [TestMethod]
    public void LiftWaitlistModeWhenExhausted_NobodyQueuedWhileSoldOut_ClearsWaitlistMode()
    {
        // Arrange
        var sut = TicketCatalog.Create(DefaultEventId, DefaultTeamId);
        var id = TicketTypeId.New();
        sut.AddTicketType(id, TicketTypeName.From("General"), [], 1, waitlistEnabled: true);
        sut.Claim([id], ClaimMode.Public); // WaitlistMode on

        // Act
        sut.LiftWaitlistModeWhenExhausted(id);

        // Assert
        sut.GetTicketType(id)!.WaitlistMode.ShouldBeFalse();
    }

    // Given a ticket type in waitlist mode with an attendee the catalog still counts as queued
    // When waitlist mode is lifted because the waitlist is exhausted
    // Then waitlist mode remains active
    [TestMethod]
    public void LiftWaitlistModeWhenExhausted_AttendeeQueued_KeepsWaitlistMode()
    {
        // Arrange — e.g. an attendee joined concurrently after the waitlist reported itself exhausted
        var sut = TicketCatalog.Create(DefaultEventId, DefaultTeamId);
        var id = TicketTypeId.New();
        sut.AddTicketType(id, TicketTypeName.From("General"), [], 1, waitlistEnabled: true);
        sut.Claim([id], ClaimMode.Public); // WaitlistMode on
        sut.JoinWaitlistQueue(id);

        // Act
        sut.LiftWaitlistModeWhenExhausted(id);

        // Assert
        sut.GetTicketType(id)!.WaitlistMode.ShouldBeTrue();
    }

    // Given a ticket type in waitlist mode with a waitlist offer outstanding
    // When waitlist mode is lifted because the waitlist is exhausted
    // Then waitlist mode remains active
    [TestMethod]
    public void LiftWaitlistModeWhenExhausted_OfferOutstanding_KeepsWaitlistMode()
    {
        // Arrange
        var sut = TicketCatalog.Create(DefaultEventId, DefaultTeamId);
        var id = TicketTypeId.New();
        sut.AddTicketType(id, TicketTypeName.From("General"), [], 1, waitlistEnabled: true);
        sut.Claim([id], ClaimMode.Public); // WaitlistMode on
        sut.HoldForWaitlistOffer(id);

        // Act
        sut.LiftWaitlistModeWhenExhausted(id);

        // Assert
        sut.GetTicketType(id)!.WaitlistMode.ShouldBeTrue();
    }

    // Given a ticket type with two attendees queued
    // When one leaves, and then leaving is counted twice more
    // Then the queued count goes down to zero and stays there
    [TestMethod]
    public void LeaveWaitlistQueue_MoreLeavesThanJoins_ClampsAtZero()
    {
        // Arrange
        var sut = TicketCatalog.Create(DefaultEventId, DefaultTeamId);
        var id = TicketTypeId.New();
        sut.AddTicketType(id, TicketTypeName.From("General"), [], 2, waitlistEnabled: true);
        sut.JoinWaitlistQueue(id);
        sut.JoinWaitlistQueue(id);

        // Act
        sut.LeaveWaitlistQueue(id);
        var afterOneLeave = sut.GetTicketType(id)!.WaitlistQueuedCount;
        sut.LeaveWaitlistQueue(id);
        sut.LeaveWaitlistQueue(id);

        // Assert
        afterOneLeave.ShouldBe(1);
        sut.GetTicketType(id)!.WaitlistQueuedCount.ShouldBe(0);
    }

    // Given a catalog without the ticket type
    // When leaving its waitlist queue is counted
    // Then nothing happens
    [TestMethod]
    public void LeaveWaitlistQueue_UnknownTicketType_IsIgnored()
    {
        // Arrange
        var sut = TicketCatalog.Create(DefaultEventId, DefaultTeamId);

        // Act & Assert
        Should.NotThrow(() => sut.LeaveWaitlistQueue(TicketTypeId.New()));
    }

    // Given an archived event
    // When an attendee joining a ticket type's waitlist queue is counted
    // Then it throws EventNotActive
    [TestMethod]
    public void JoinWaitlistQueue_EventArchived_ThrowsEventNotActive()
    {
        // Arrange
        var sut = TicketCatalog.Create(DefaultEventId, DefaultTeamId);
        var id = TicketTypeId.New();
        sut.AddTicketType(id, TicketTypeName.From("General"), [], 2, waitlistEnabled: true);
        sut.MarkEventArchived();

        // Act
        var result = ErrorResult.Capture(() => sut.JoinWaitlistQueue(id));

        // Assert
        result.Error.ShouldMatch(TicketCatalog.Errors.EventNotActive);
    }

    // ─── Waitlist-held capacity ───────────────────────────────────────────────

    /// <summary>
    /// A waitlist-enabled ticket type with <paramref name="capacity"/> seats, all claimed publicly, so it is sold out
    /// and in WaitlistMode. Returns the claimed tickets.
    /// </summary>
    private static List<IReadOnlyList<TicketTypeSnapshot>> SellOut(TicketCatalog sut, TicketTypeId id, int capacity)
    {
        sut.AddTicketType(id, TicketTypeName.From("General"), [], capacity, waitlistEnabled: true);
        var tickets = Enumerable.Range(0, capacity).Select(_ => sut.Claim([id], ClaimMode.Public)).ToList();
        sut.ClearDomainEvents();
        return tickets;
    }

    // Given a ticket type with seats available
    // When a waitlist offer holds one of them
    // Then that seat is no longer available to the public
    [TestMethod]
    public void HoldForWaitlistOffer_SeatAvailable_ReducesPublicAvailability()
    {
        // Arrange — 2 seats, 1 used
        var sut = TicketCatalog.Create(DefaultEventId, DefaultTeamId);
        var id = TicketTypeId.New();
        sut.AddTicketType(id, TicketTypeName.From("General"), [], 2, waitlistEnabled: true);
        sut.Claim([id], ClaimMode.Public);

        // Act
        sut.HoldForWaitlistOffer(id);

        // Assert
        var ticketType = sut.GetTicketType(id)!;
        ticketType.WaitlistHeldCapacity.ShouldBe(1);
        ticketType.AvailableCapacity.ShouldBe(0);
        ticketType.PublicAvailableCapacity.ShouldBe(0);
        ticketType.IsSoldOut.ShouldBeTrue();
    }

    // Given a ticket type with admin tickets, public tickets and an automatic waitlist offer outstanding
    // When its availability is read
    // Then only the public tickets and the hold are subtracted from the public capacity
    [TestMethod]
    public void AvailableCapacity_WithAdminTicketsAndHold_SubtractsPublicUsedAndHeldOnly()
    {
        // Arrange — 10 public seats, 3 admin tickets, 4 public used, 1 held
        var sut = TicketCatalog.Create(DefaultEventId, DefaultTeamId);
        var id = TicketTypeId.New();
        sut.AddTicketType(id, TicketTypeName.From("General"), [], 10, waitlistEnabled: true);
        for (var i = 0; i < 3; i++)
            sut.Claim([id], ClaimMode.Admin);
        for (var i = 0; i < 4; i++)
            sut.Claim([id], ClaimMode.Public);

        // Act
        sut.HoldForWaitlistOffer(id);

        // Assert — 10 − 4 used − 1 held
        sut.GetTicketType(id)!.AvailableCapacity.ShouldBe(5);
    }

    // Given a ticket type without a capacity limit
    // When its availability is read
    // Then it is unbounded
    [TestMethod]
    public void AvailableCapacity_UnboundedCapacity_IsNull()
    {
        // Arrange
        var sut = TicketCatalog.Create(DefaultEventId, DefaultTeamId);
        var id = TicketTypeId.New();
        sut.AddTicketType(id, TicketTypeName.From("General"), [], publicCapacity: null);

        // Act
        sut.HoldForWaitlistOffer(id);

        // Assert
        sut.GetTicketType(id)!.AvailableCapacity.ShouldBeNull();
    }

    // Given an outstanding automatic waitlist offer
    // When its coupon is redeemed
    // Then its hold turns into a public ticket and the availability is unchanged
    [TestMethod]
    public void ClaimWithCoupon_AutomaticWaitlistOffer_ConvertsHoldIntoPublicTicket()
    {
        // Arrange — 2 seats, 1 used, 1 held
        var sut = TicketCatalog.Create(DefaultEventId, DefaultTeamId);
        var id = TicketTypeId.New();
        sut.AddTicketType(id, TicketTypeName.From("General"), [], 2, waitlistEnabled: true);
        sut.Claim([id], ClaimMode.Public);
        sut.HoldForWaitlistOffer(id);
        var coupon = new CouponBuilder()
            .WithRequestedTicketTypeIds(id)
            .WithAvailableTicketTypes(new TicketTypeInfo(id))
            .WithWaitlistOrigin(WaitlistCouponOrigin.Automatic)
            .Build();

        // Act
        var tickets = sut.ClaimWithCoupon([id], coupon);

        // Assert
        var ticketType = sut.GetTicketType(id)!;
        ticketType.WaitlistHeldCapacity.ShouldBe(0);
        ticketType.PublicUsedCapacity.ShouldBe(2);
        ticketType.AdminUsedCount.ShouldBe(0);
        ticketType.AvailableCapacity.ShouldBe(0);
        tickets.ShouldHaveSingleItem().Mode.ShouldBe(ClaimMode.Public);
    }

    // Given a sold-out ticket type
    // When a VIP waitlist coupon or an organiser coupon is redeemed
    // Then it claims an admin ticket and leaves the public counters unchanged
    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void ClaimWithCoupon_VipOrOrganiserCoupon_ClaimsAdminTicket(bool vip)
    {
        // Arrange
        var sut = TicketCatalog.Create(DefaultEventId, DefaultTeamId);
        var id = TicketTypeId.New();
        SellOut(sut, id, capacity: 1);
        var builder = new CouponBuilder()
            .WithRequestedTicketTypeIds(id)
            .WithAvailableTicketTypes(new TicketTypeInfo(id));
        var coupon = (vip ? builder.WithWaitlistOrigin(WaitlistCouponOrigin.Manual) : builder).Build();

        // Act
        var tickets = sut.ClaimWithCoupon([id], coupon);

        // Assert
        var ticketType = sut.GetTicketType(id)!;
        ticketType.AdminUsedCount.ShouldBe(1);
        ticketType.PublicUsedCapacity.ShouldBe(1);
        ticketType.WaitlistHeldCapacity.ShouldBe(0);
        ticketType.AvailableCapacity.ShouldBe(0);
        tickets.ShouldHaveSingleItem().Mode.ShouldBe(ClaimMode.Admin);
    }

    // Given a sold-out ticket type in waitlist mode with an automatic offer outstanding
    // When the offer lapses and its hold is released
    // Then a WaitlistCapacityAvailable event reports the seat for the next person waiting
    [TestMethod]
    public void ReleaseWaitlistHold_SeatLeftInWaitlistMode_RaisesWaitlistCapacityAvailableEvent()
    {
        // Arrange — 2 seats sold out, one released and immediately held by an offer
        var sut = TicketCatalog.Create(DefaultEventId, DefaultTeamId);
        var id = TicketTypeId.New();
        var tickets = SellOut(sut, id, capacity: 2);
        sut.Release(tickets[0]);
        sut.HoldForWaitlistOffer(id);
        sut.ClearDomainEvents();

        // Act
        sut.ReleaseWaitlistHold(id);

        // Assert
        sut.GetTicketType(id)!.WaitlistHeldCapacity.ShouldBe(0);
        var evt = sut.GetDomainEvents().OfType<WaitlistCapacityAvailableDomainEvent>().ShouldHaveSingleItem();
        evt.TicketTypeId.ShouldBe(id);
        evt.AvailableCapacity.ShouldBe(1);
    }

    // Given an automatic offer outstanding on a ticket type whose public capacity was then lowered below what's committed
    // When the offer lapses and its hold is released
    // Then the released seat only makes up the shortfall, so no event is raised
    [TestMethod]
    public void ReleaseWaitlistHold_PublicCapacityLoweredBelowCommitted_RaisesNoEvent()
    {
        // Arrange — 2 seats sold out, one released and held by an offer, then capacity lowered to 1
        var sut = TicketCatalog.Create(DefaultEventId, DefaultTeamId);
        var id = TicketTypeId.New();
        var tickets = SellOut(sut, id, capacity: 2);
        sut.Release(tickets[0]);
        sut.HoldForWaitlistOffer(id);
        sut.UpdateTicketType(id, name: null, publicCapacity: 1);
        sut.ClearDomainEvents();

        // Act
        sut.ReleaseWaitlistHold(id);

        // Assert
        sut.GetTicketType(id)!.AvailableCapacity.ShouldBe(0);
        sut.GetDomainEvents().ShouldBeEmpty();
    }

    // Given a ticket type without any outstanding waitlist offer
    // When a hold is released
    // Then the held capacity stays at zero
    [TestMethod]
    public void ReleaseWaitlistHold_NoHold_ClampsAtZero()
    {
        // Arrange
        var sut = TicketCatalog.Create(DefaultEventId, DefaultTeamId);
        var id = TicketTypeId.New();
        sut.AddTicketType(id, TicketTypeName.From("General"), [], 2, waitlistEnabled: true);

        // Act
        sut.ReleaseWaitlistHold(id);

        // Assert
        sut.GetTicketType(id)!.WaitlistHeldCapacity.ShouldBe(0);
    }

    // Given a sold-out ticket type in waitlist mode
    // When a ticket is released
    // Then a WaitlistCapacityAvailable event reports the freed seat
    [TestMethod]
    public void Release_SeatLeftInWaitlistMode_RaisesWaitlistCapacityAvailableEvent()
    {
        // Arrange
        var sut = TicketCatalog.Create(DefaultEventId, DefaultTeamId);
        var id = TicketTypeId.New();
        var tickets = SellOut(sut, id, capacity: 2);

        // Act
        sut.Release(tickets[0]);

        // Assert
        sut.GetDomainEvents().OfType<WaitlistCapacityAvailableDomainEvent>()
            .ShouldHaveSingleItem().AvailableCapacity.ShouldBe(1);
    }

    // Given a sold-out ticket type in waitlist mode whose public capacity was lowered by one below what's used
    // When a ticket is released, then another
    // Then the first release makes up the shortfall and only the second raises a WaitlistCapacityAvailable event
    [TestMethod]
    public void Release_AfterPublicCapacityLowered_MakesUpShortfallFirst()
    {
        // Arrange
        var sut = TicketCatalog.Create(DefaultEventId, DefaultTeamId);
        var id = TicketTypeId.New();
        var tickets = SellOut(sut, id, capacity: 3);
        sut.UpdateTicketType(id, name: null, publicCapacity: 2);
        sut.ClearDomainEvents();

        // Act
        sut.Release(tickets[0]);
        var eventsAfterFirst = sut.GetDomainEvents().OfType<WaitlistCapacityAvailableDomainEvent>().Count();
        sut.Release(tickets[1]);

        // Assert
        eventsAfterFirst.ShouldBe(0);
        sut.GetDomainEvents().OfType<WaitlistCapacityAvailableDomainEvent>()
            .ShouldHaveSingleItem().AvailableCapacity.ShouldBe(1);
    }

    // Given a ticket type with a waitlist that is not in waitlist mode
    // When a ticket is released
    // Then no event is raised
    [TestMethod]
    public void Release_NotInWaitlistMode_RaisesNoEvent()
    {
        // Arrange
        var sut = TicketCatalog.Create(DefaultEventId, DefaultTeamId);
        var id = TicketTypeId.New();
        sut.AddTicketType(id, TicketTypeName.From("General"), [], 3, waitlistEnabled: true);
        var tickets = sut.Claim([id], ClaimMode.Public);
        sut.ClearDomainEvents();

        // Act
        sut.Release(tickets);

        // Assert
        sut.GetDomainEvents().ShouldBeEmpty();
    }

    // Given a sold-out ticket type in waitlist mode whose public capacity was lowered by two below what's used
    // When its public capacity is raised by three
    // Then the event reports only the one seat left after making up the shortfall
    [TestMethod]
    public void UpdateTicketType_CapacityRaisedAfterShortfall_ReportsSeatsLeftAfterShortfall()
    {
        // Arrange
        var sut = TicketCatalog.Create(DefaultEventId, DefaultTeamId);
        var id = TicketTypeId.New();
        SellOut(sut, id, capacity: 3);
        sut.UpdateTicketType(id, name: null, publicCapacity: 1);
        sut.ClearDomainEvents();

        // Act
        sut.UpdateTicketType(id, name: null, publicCapacity: 4);

        // Assert
        sut.GetDomainEvents().OfType<WaitlistCapacityAvailableDomainEvent>()
            .ShouldHaveSingleItem().AvailableCapacity.ShouldBe(1);
    }

    // Given a sold-out ticket type in waitlist mode whose public capacity was lowered by two below what's used
    // When its public capacity is raised by one
    // Then the new seat only makes up part of the shortfall, so no event is raised
    [TestMethod]
    public void UpdateTicketType_CapacityRaisedLessThanShortfall_RaisesNoEvent()
    {
        // Arrange
        var sut = TicketCatalog.Create(DefaultEventId, DefaultTeamId);
        var id = TicketTypeId.New();
        SellOut(sut, id, capacity: 3);
        sut.UpdateTicketType(id, name: null, publicCapacity: 1);
        sut.ClearDomainEvents();

        // Act
        sut.UpdateTicketType(id, name: null, publicCapacity: 2);

        // Assert
        sut.GetDomainEvents().OfType<WaitlistCapacityAvailableDomainEvent>().ShouldBeEmpty();
    }

    // Given a sold-out ticket type in waitlist mode
    // When only its name changes
    // Then no event is raised
    [TestMethod]
    public void UpdateTicketType_NameOnlyInWaitlistMode_RaisesNoWaitlistCapacityAvailableEvent()
    {
        // Arrange
        var sut = TicketCatalog.Create(DefaultEventId, DefaultTeamId);
        var id = TicketTypeId.New();
        SellOut(sut, id, capacity: 1);

        // Act
        sut.UpdateTicketType(id, TicketTypeName.From("Renamed"), publicCapacity: 1);

        // Assert
        sut.GetDomainEvents().OfType<WaitlistCapacityAvailableDomainEvent>().ShouldBeEmpty();
    }
}
