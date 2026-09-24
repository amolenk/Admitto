using Amolenk.Admitto.Core.Registrations.Domain.DomainEvents;
using Amolenk.Admitto.Core.Registrations.Domain.Entities;
using Amolenk.Admitto.Core.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;
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
        tt.MaxCapacity.ShouldBe(100);
        tt.UsedCapacity.ShouldBe(0);
    }

    // When a ticket type is added without a maximum capacity
    // Then its maximum capacity is null
    [TestMethod]
    public void AddTicketType_NoCapacity_SetsNullMaxCapacity()
    {
        // Arrange
        var sut = TicketCatalog.Create(DefaultEventId, DefaultTeamId);

        // Act
        sut.AddTicketType(
            TicketTypeId.New(),
            TicketTypeName.From("Speaker Pass"),
            [],
            maxCapacity: null);

        // Assert
        sut.TicketTypes[0].MaxCapacity.ShouldBeNull();
    }

    // When a ticket type is added with a reserved capacity within bounds
    // Then it is added with the given reserved capacity
    [TestMethod]
    public void AddTicketType_WithReservedCapacity_SetsReservedCapacity()
    {
        // Arrange
        var sut = TicketCatalog.Create(DefaultEventId, DefaultTeamId);
        var id = TicketTypeId.New();

        // Act
        sut.AddTicketType(id, TicketTypeName.From("General"), [], maxCapacity: 100, reservedCapacity: 20);

        // Assert
        sut.TicketTypes[0].ReservedCapacity.ShouldBe(20);
    }

    // When a ticket type is added with reserved capacity but no maximum capacity
    // Then it throws ReservedCapacityRequiresBoundedCapacity
    [TestMethod]
    public void AddTicketType_ReservedCapacityWithoutMaxCapacity_Throws()
    {
        // Arrange
        var sut = TicketCatalog.Create(DefaultEventId, DefaultTeamId);
        var id = TicketTypeId.New();

        // Act
        var result = ErrorResult.Capture(() =>
            sut.AddTicketType(id, TicketTypeName.From("General"), [], maxCapacity: null, reservedCapacity: 5));

        // Assert
        result.Error.ShouldMatch(TicketCatalog.Errors.ReservedCapacityRequiresBoundedCapacity(id));
    }

    // When a ticket type is added with reserved capacity greater than its maximum capacity
    // Then it throws ReservedCapacityExceedsCapacity
    [TestMethod]
    public void AddTicketType_ReservedCapacityExceedsMaxCapacity_Throws()
    {
        // Arrange
        var sut = TicketCatalog.Create(DefaultEventId, DefaultTeamId);
        var id = TicketTypeId.New();

        // Act
        var result = ErrorResult.Capture(() =>
            sut.AddTicketType(id, TicketTypeName.From("General"), [], maxCapacity: 10, reservedCapacity: 11));

        // Assert
        result.Error.ShouldMatch(TicketCatalog.Errors.ReservedCapacityExceedsCapacity(id));
    }

    // When a ticket type is added with a negative reserved capacity
    // Then it throws ReservedCapacityNegative
    [TestMethod]
    public void AddTicketType_NegativeReservedCapacity_Throws()
    {
        // Arrange
        var sut = TicketCatalog.Create(DefaultEventId, DefaultTeamId);
        var id = TicketTypeId.New();

        // Act
        var result = ErrorResult.Capture(() =>
            sut.AddTicketType(id, TicketTypeName.From("General"), [], maxCapacity: 10, reservedCapacity: -1));

        // Assert
        result.Error.ShouldMatch(TicketCatalog.Errors.ReservedCapacityNegative(id));
    }

    // Given reserved capacity that equals the entire maximum capacity of a waitlist-enabled type
    // When the ticket type is added
    // Then it is immediately publicly sold out, activating waitlist mode and raising the event
    [TestMethod]
    public void AddTicketType_ReservedCapacityEqualsMaxCapacityWithWaitlistEnabled_ActivatesWaitlistModeImmediately()
    {
        // Arrange
        var sut = TicketCatalog.Create(DefaultEventId, DefaultTeamId);
        var id = TicketTypeId.New();

        // Act — all 5 slots reserved, none left for the public
        sut.AddTicketType(id, TicketTypeName.From("General"), [], maxCapacity: 5, waitlistEnabled: true,
            reservedCapacity: 5);

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
        sut.UpdateTicketType(id, name: null, maxCapacity: 200);

        // Assert
        sut.TicketTypes[0].MaxCapacity.ShouldBe(200);
    }

    // Given an existing ticket type
    // When its reserved capacity is updated
    // Then the new reserved capacity is applied
    [TestMethod]
    public void UpdateTicketType_ReservedCapacity_Updates()
    {
        // Arrange
        var sut = TicketCatalog.Create(DefaultEventId, DefaultTeamId);
        var id = TicketTypeId.New();
        sut.AddTicketType(id, TicketTypeName.From("VIP"), [], maxCapacity: 100, reservedCapacity: 10);

        // Act
        sut.UpdateTicketType(id, name: null, maxCapacity: 100, reservedCapacity: 30);

        // Assert
        sut.TicketTypes[0].ReservedCapacity.ShouldBe(30);
    }

    // Given a ticket type whose public slots are all used but total capacity still has room
    // When reserved capacity is increased via an update to cover the remaining room
    // Then waitlist mode activates immediately and a WaitlistModeActivated event is raised
    [TestMethod]
    public void UpdateTicketType_IncreaseReservedCapacityFillsPublicThreshold_ActivatesWaitlistMode()
    {
        // Arrange — 8 of 10 used, waitlist enabled, 2 public slots still free
        var sut = TicketCatalog.Create(DefaultEventId, DefaultTeamId);
        var id = TicketTypeId.New();
        sut.AddTicketType(id, TicketTypeName.From("General"), [], maxCapacity: 10, waitlistEnabled: true);
        for (var i = 0; i < 8; i++)
            sut.Claim([id], ClaimMode.Public);

        // Act — reserve the 2 remaining public slots
        sut.UpdateTicketType(id, name: null, maxCapacity: 10, reservedCapacity: 2);

        // Assert
        var tt = sut.GetTicketType(id)!;
        tt.WaitlistMode.ShouldBeTrue();
        sut.GetDomainEvents().OfType<WaitlistModeActivatedDomainEvent>()
            .ShouldHaveSingleItem()
            .TicketTypeId.ShouldBe(id);
    }

    // Given a ticket type that is publicly sold out and in waitlist mode because of reserved capacity
    // When reserved capacity is decreased via an update
    // Then a WaitlistCapacityFreed event is raised reporting the newly freed public slots
    [TestMethod]
    public void UpdateTicketType_DecreaseReservedCapacityWhileInWaitlistMode_RaisesWaitlistCapacityFreedEvent()
    {
        // Arrange — 8 of 10 used, 2 reserved → publicly sold out, waitlist mode on
        var sut = TicketCatalog.Create(DefaultEventId, DefaultTeamId);
        var id = TicketTypeId.New();
        sut.AddTicketType(id, TicketTypeName.From("General"), [], maxCapacity: 10, waitlistEnabled: true,
            reservedCapacity: 2);
        for (var i = 0; i < 8; i++)
            sut.Claim([id], ClaimMode.Public);
        sut.ClearDomainEvents();

        // Act — free up the 2 reserved slots
        sut.UpdateTicketType(id, name: null, maxCapacity: 10, reservedCapacity: 0);

        // Assert
        var evt = sut.GetDomainEvents().OfType<WaitlistCapacityFreedDomainEvent>()
            .ShouldHaveSingleItem();
        evt.TicketTypeId.ShouldBe(id);
        evt.FreedSlots.ShouldBe(2);
    }

    // Given a ticket type with reserved capacity where an admin claims tickets before any public sales
    // When public claims subsequently fill the remaining public pool
    // Then the full public pool remains sellable — the early reserved claim does not shrink it
    [TestMethod]
    public void Claim_ReservedInterleavedBeforePublicClaims_DoesNotReducePublicAvailability()
    {
        // Arrange — 200 max, 20 reserved, admin claims 10 seats before public sales open
        var sut = TicketCatalog.Create(DefaultEventId, DefaultTeamId);
        var id = TicketTypeId.New();
        sut.AddTicketType(id, TicketTypeName.From("General"), [], maxCapacity: 200, reservedCapacity: 20);
        for (var i = 0; i < 10; i++)
            sut.Claim([id], ClaimMode.Reserved);

        // Act — sell the full remaining public pool (180 seats)
        for (var i = 0; i < 180; i++)
            sut.Claim([id], ClaimMode.Public);

        // Assert — sold out only now, not before the 180th public claim
        sut.GetTicketType(id)!.IsSoldOut.ShouldBeTrue();
        sut.GetTicketType(id)!.UsedCapacity.ShouldBe(190);
    }

    // Given a claim made under the general reserved pool
    // When that ticket is later released using its recorded claim mode
    // Then the reserved buffer is credited back, not just the total used capacity
    [TestMethod]
    public void Release_ReservedClaimSnapshot_CreditsReservedBufferBack()
    {
        // Arrange
        var sut = TicketCatalog.Create(DefaultEventId, DefaultTeamId);
        var id = TicketTypeId.New();
        sut.AddTicketType(id, TicketTypeName.From("General"), [], maxCapacity: 10, reservedCapacity: 2);
        var tickets = sut.Claim([id], ClaimMode.Reserved);

        // Act
        sut.Release(tickets);

        // Assert
        var tt = sut.GetTicketType(id)!;
        tt.UsedCapacity.ShouldBe(0);
        tt.ReservedUsedCapacity.ShouldBe(0);
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
        sut.UpdateTicketType(id, name: TicketTypeName.From("VIP Access"), maxCapacity: 100);

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
            sut.UpdateTicketType(unknownId, name: null, maxCapacity: 100));

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
        sut.TicketTypes[0].UsedCapacity.ShouldBe(1);
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
        sut.TicketTypes[0].UsedCapacity.ShouldBe(1);
    }

    // Given a ticket type already at capacity
    // When a ticket is claimed without enforcement
    // Then the used capacity still increments beyond the capacity limit
    [TestMethod]
    public void Claim_Uncapped_AlwaysIncrements()
    {
        // Arrange
        var sut = TicketCatalog.Create(DefaultEventId, DefaultTeamId);
        var id = TicketTypeId.New();
        sut.AddTicketType(id, TicketTypeName.From("VIP"), [], 1);
        sut.Claim([id], ClaimMode.Reserved); // at capacity

        // Act
        sut.Claim([id], ClaimMode.Reserved); // should still work

        // Assert
        sut.TicketTypes[0].UsedCapacity.ShouldBe(2);
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
        sut.TicketTypes.Single(t => t.Id == idA).UsedCapacity.ShouldBe(1);
        sut.TicketTypes.Single(t => t.Id == idB).UsedCapacity.ShouldBe(1);
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
        var result = ErrorResult.Capture(() => sut.Claim([id], ClaimMode.Reserved));

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
        sut.GetTicketType(id)!.UsedCapacity.ShouldBe(1);

        // Act
        sut.Release(tickets);

        // Assert
        sut.GetTicketType(id)!.UsedCapacity.ShouldBe(0);
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
        sut.GetTicketType(knownId)!.UsedCapacity.ShouldBe(0);
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
        sut.GetTicketType(idA)!.UsedCapacity.ShouldBe(0);
        sut.GetTicketType(idB)!.UsedCapacity.ShouldBe(0);
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
        var result = ErrorResult.Capture(() => sut.Claim([id, id], ClaimMode.Reserved));

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
        var result = ErrorResult.Capture(() => sut.Claim([idA, idB], ClaimMode.Reserved));

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
        sut.GetTicketType(id)!.UsedCapacity.ShouldBe(0);
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
        sut.Claim([id], ClaimMode.Reserved);

        // Assert
        sut.GetTicketType(id)!.UsedCapacity.ShouldBe(1);
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
        sut.Claim([id], ClaimMode.Reserved);

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
        sut.Claim([id], ClaimMode.Reserved);
        sut.Claim([id], ClaimMode.Reserved);

        // Act
        sut.UpdateTicketType(id, name: null, maxCapacity: 2, waitlistEnabled: true);

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
        sut.Claim([id], ClaimMode.Reserved);

        // Act
        sut.UpdateTicketType(id, name: null, maxCapacity: 2, waitlistEnabled: true);

        // Assert
        sut.GetTicketType(id)!.WaitlistMode.ShouldBeFalse();
        sut.GetDomainEvents().OfType<WaitlistModeActivatedDomainEvent>().ShouldBeEmpty();
    }

    // Given a ticket type currently in waitlist mode
    // When waitlist is disabled for it via an update
    // Then waitlist mode is forced off and a WaitlistForcedDisabled event is raised
    [TestMethod]
    public void UpdateTicketType_DisableWaitlistWhileInWaitlistMode_ForcesDisableAndRaisesEvent()
    {
        // Arrange — waitlist mode active
        var sut = TicketCatalog.Create(DefaultEventId, DefaultTeamId);
        var id = TicketTypeId.New();
        sut.AddTicketType(id, TicketTypeName.From("General"), [], 1, waitlistEnabled: true);
        sut.Claim([id], ClaimMode.Public); // fills capacity → WaitlistMode activates

        // Act
        sut.UpdateTicketType(id, name: null, maxCapacity: 1, waitlistEnabled: false);

        // Assert
        var tt = sut.GetTicketType(id)!;
        tt.WaitlistEnabled.ShouldBeFalse();
        tt.WaitlistMode.ShouldBeFalse();
        sut.GetDomainEvents().OfType<WaitlistForcedDisabledDomainEvent>()
            .ShouldHaveSingleItem()
            .TicketTypeId.ShouldBe(id);
    }

    // Given a ticket type with waitlist enabled and a bounded capacity
    // When the capacity limit is removed via an update
    // Then waitlist is forced off, waitlist mode clears, and a WaitlistForcedDisabled event is raised
    [TestMethod]
    public void UpdateTicketType_RemoveCapacityLimitWithWaitlistEnabled_ForcesDisableAndRaisesEvent()
    {
        // Removing the capacity bound requires force-disabling waitlist
        var sut = TicketCatalog.Create(DefaultEventId, DefaultTeamId);
        var id = TicketTypeId.New();
        sut.AddTicketType(id, TicketTypeName.From("General"), [], 10, waitlistEnabled: true);

        // Act — remove capacity limit (set null)
        sut.UpdateTicketType(id, name: null, maxCapacity: null);

        // Assert
        var tt = sut.GetTicketType(id)!;
        tt.WaitlistEnabled.ShouldBeFalse();
        tt.WaitlistMode.ShouldBeFalse();
        tt.MaxCapacity.ShouldBeNull();
        sut.GetDomainEvents().OfType<WaitlistForcedDisabledDomainEvent>()
            .ShouldHaveSingleItem()
            .TicketTypeId.ShouldBe(id);
    }

    // Given a ticket type that is sold out and in waitlist mode
    // When its capacity is increased via an update
    // Then a WaitlistCapacityFreed event is raised reporting the number of freed slots
    [TestMethod]
    public void UpdateTicketType_CapacityIncreaseWhileInWaitlistMode_RaisesWaitlistCapacityFreedEvent()
    {
        // Arrange — sold out and in WaitlistMode
        var sut = TicketCatalog.Create(DefaultEventId, DefaultTeamId);
        var id = TicketTypeId.New();
        sut.AddTicketType(id, TicketTypeName.From("General"), [], 1, waitlistEnabled: true);
        sut.Claim([id], ClaimMode.Public); // fills to capacity → WaitlistMode on
        sut.ClearDomainEvents();

        // Act — add 3 more slots
        sut.UpdateTicketType(id, name: null, maxCapacity: 4);

        // Assert — 3 freed slots
        var evt = sut.GetDomainEvents().OfType<WaitlistCapacityFreedDomainEvent>()
            .ShouldHaveSingleItem();
        evt.TicketTypeId.ShouldBe(id);
        evt.FreedSlots.ShouldBe(3);
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
            sut.AddTicketType(id, TicketTypeName.From("General"), [], maxCapacity: null, waitlistEnabled: true));

        // Assert
        result.Error.ShouldMatch(TicketCatalog.Errors.WaitlistRequiresBoundedCapacity(id));
    }

    // Given a ticket type in waitlist mode where used capacity is below total capacity but still at the public threshold
    // When waitlist mode is re-evaluated with no active entries and no issued coupons
    // Then waitlist mode remains active because the public threshold is still met
    [TestMethod]
    public void ReEvaluateWaitlistMode_BelowTotalCapacityButAtPublicThreshold_KeepsWaitlistMode()
    {
        // Arrange — 1 of 2 used, 1 reserved → public threshold is 1, so still publicly sold out
        var sut = TicketCatalog.Create(DefaultEventId, DefaultTeamId);
        var id = TicketTypeId.New();
        sut.AddTicketType(id, TicketTypeName.From("General"), [], 2, waitlistEnabled: true, reservedCapacity: 1);
        sut.Claim([id], ClaimMode.Public); // 1 of 2 used → publicly sold out (threshold 1) → WaitlistMode on

        // Act — no active entries, no issued coupons
        sut.ReEvaluateWaitlistMode(id, activeEntryCount: 0, issuedCouponCount: 0);

        // Assert — a naive UsedCapacity < MaxCapacity check would wrongly clear WaitlistMode here
        sut.GetTicketType(id)!.WaitlistMode.ShouldBeTrue();
    }

    // Given a ticket type in waitlist mode with a slot now freed
    // When waitlist mode is re-evaluated with no active entries and no issued coupons
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

        // Act — no active entries, no issued coupons
        sut.ReEvaluateWaitlistMode(id, activeEntryCount: 0, issuedCouponCount: 0);

        // Assert
        sut.GetTicketType(id)!.WaitlistMode.ShouldBeFalse();
    }

    // Given a ticket type in waitlist mode that is still at capacity
    // When waitlist mode is re-evaluated with no active entries and no issued coupons
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
        sut.ReEvaluateWaitlistMode(id, activeEntryCount: 0, issuedCouponCount: 0);

        // Assert — still at capacity → stays in WaitlistMode
        sut.GetTicketType(id)!.WaitlistMode.ShouldBeTrue();
    }

    // Given a ticket type in waitlist mode with a slot freed but active waitlist entries remaining
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

        // Act — entries still active
        sut.ReEvaluateWaitlistMode(id, activeEntryCount: 1, issuedCouponCount: 0);

        // Assert
        sut.GetTicketType(id)!.WaitlistMode.ShouldBeTrue();
    }

    // Given a ticket type in waitlist mode with a slot freed but a coupon still outstanding
    // When waitlist mode is re-evaluated
    // Then waitlist mode remains active
    [TestMethod]
    public void ReEvaluateWaitlistMode_IssuedCouponsRemaining_KeepsWaitlistMode()
    {
        // Arrange — capacity freed but coupon still outstanding
        var sut = TicketCatalog.Create(DefaultEventId, DefaultTeamId);
        var id = TicketTypeId.New();
        sut.AddTicketType(id, TicketTypeName.From("General"), [], 2, waitlistEnabled: true);
        sut.Claim([id], ClaimMode.Public);
        var tickets = sut.Claim([id], ClaimMode.Public); // WaitlistMode on
        sut.Release(tickets); // one slot freed

        // Act — coupon still in flight
        sut.ReEvaluateWaitlistMode(id, activeEntryCount: 0, issuedCouponCount: 1);

        // Assert
        sut.GetTicketType(id)!.WaitlistMode.ShouldBeTrue();
    }

    // Given a ticket type in waitlist mode with a slot now freed
    // When waitlist mode deactivation is attempted
    // Then waitlist mode is cleared
    [TestMethod]
    public void TryDeactivateWaitlistMode_WhenCapacityAvailable_ClearsWaitlistMode()
    {
        // Arrange — sold out → WaitlistMode on, then one slot freed
        var sut = TicketCatalog.Create(DefaultEventId, DefaultTeamId);
        var id = TicketTypeId.New();
        sut.AddTicketType(id, TicketTypeName.From("General"), [], 2, waitlistEnabled: true);
        sut.Claim([id], ClaimMode.Public);
        var tickets = sut.Claim([id], ClaimMode.Public); // WaitlistMode on
        sut.Release(tickets); // UsedCapacity < MaxCapacity now

        // Act
        sut.TryDeactivateWaitlistMode(id);

        // Assert
        sut.GetTicketType(id)!.WaitlistMode.ShouldBeFalse();
    }

    // Given a ticket type in waitlist mode that is still at capacity
    // When waitlist mode deactivation is attempted
    // Then waitlist mode remains active
    [TestMethod]
    public void TryDeactivateWaitlistMode_WhenAtCapacity_DoesNotClearWaitlistMode()
    {
        // Arrange — sold out → WaitlistMode on, still at capacity
        var sut = TicketCatalog.Create(DefaultEventId, DefaultTeamId);
        var id = TicketTypeId.New();
        sut.AddTicketType(id, TicketTypeName.From("General"), [], 1, waitlistEnabled: true);
        sut.Claim([id], ClaimMode.Public); // WaitlistMode on, UsedCapacity == MaxCapacity

        // Act
        sut.TryDeactivateWaitlistMode(id);

        // Assert
        sut.GetTicketType(id)!.WaitlistMode.ShouldBeTrue();
    }

    // Given a ticket type in waitlist mode where used capacity is below total capacity but still at the public threshold
    // When waitlist mode deactivation is attempted
    // Then waitlist mode remains active because the public threshold is still met
    [TestMethod]
    public void TryDeactivateWaitlistMode_BelowTotalCapacityButAtPublicThreshold_DoesNotClearWaitlistMode()
    {
        // Arrange — 1 of 2 used, 1 reserved → public threshold is 1, so still publicly sold out
        var sut = TicketCatalog.Create(DefaultEventId, DefaultTeamId);
        var id = TicketTypeId.New();
        sut.AddTicketType(id, TicketTypeName.From("General"), [], 2, waitlistEnabled: true, reservedCapacity: 1);
        sut.Claim([id], ClaimMode.Public); // 1 of 2 used → publicly sold out (threshold 1) → WaitlistMode on

        // Act — a naive UsedCapacity < MaxCapacity check would wrongly clear WaitlistMode here
        sut.TryDeactivateWaitlistMode(id);

        // Assert
        sut.GetTicketType(id)!.WaitlistMode.ShouldBeTrue();
    }
}
