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

    [TestMethod]
    public void SC001_AddTicketType_ActiveEvent_AddsSuccessfully()
    {
        // Arrange
        var sut = TicketCatalog.Create(DefaultEventId);
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
        tt.IsCancelled.ShouldBeFalse();
    }

    [TestMethod]
    public void SC002_AddTicketType_NoCapacity_SetsNullMaxCapacity()
    {
        // Arrange
        var sut = TicketCatalog.Create(DefaultEventId);

        // Act
        sut.AddTicketType(
            TicketTypeId.New(),
            TicketTypeName.From("Speaker Pass"),
            [],
            maxCapacity: null);

        // Assert
        sut.TicketTypes[0].MaxCapacity.ShouldBeNull();
    }

    [TestMethod]
    public void SC003_AddTicketType_DuplicateName_Throws()
    {
        // Arrange
        var sut = TicketCatalog.Create(DefaultEventId);
        sut.AddTicketType(TicketTypeId.New(), TicketTypeName.From("VIP"), [], 100);

        // Act
        var result = ErrorResult.Capture(() =>
            sut.AddTicketType(TicketTypeId.New(), TicketTypeName.From("VIP"), [], 50));

        // Assert
        result.Error.ShouldMatch(TicketCatalog.Errors.DuplicateTicketTypeName(TicketTypeName.From("VIP")));
    }

    [TestMethod]
    public void SC004_UpdateTicketType_Capacity_Updates()
    {
        // Arrange
        var sut = TicketCatalog.Create(DefaultEventId);
        var id = TicketTypeId.New();
        sut.AddTicketType(id, TicketTypeName.From("VIP"), [], 100);

        // Act
        sut.UpdateTicketType(id, name: null, maxCapacity: 200);

        // Assert
        sut.TicketTypes[0].MaxCapacity.ShouldBe(200);
    }

    [TestMethod]
    public void SC005_UpdateTicketType_Name_Updates()
    {
        // Arrange
        var sut = TicketCatalog.Create(DefaultEventId);
        var id = TicketTypeId.New();
        sut.AddTicketType(id, TicketTypeName.From("VIP"), [], 100);

        // Act
        sut.UpdateTicketType(id, name: TicketTypeName.From("VIP Access"), maxCapacity: 100);

        // Assert
        sut.TicketTypes[0].Name.ShouldBe(TicketTypeName.From("VIP Access"));
    }

    [TestMethod]
    public void SC006_UpdateTicketType_NotFound_Throws()
    {
        // Arrange
        var sut = TicketCatalog.Create(DefaultEventId);
        var unknownId = TicketTypeId.New();

        // Act
        var result = ErrorResult.Capture(() =>
            sut.UpdateTicketType(unknownId, name: null, maxCapacity: 100));

        // Assert
        result.Error.ShouldMatch(TicketCatalog.Errors.TicketTypeNotFound(unknownId));
    }

    [TestMethod]
    public void SC007_UpdateTicketType_CancelledType_Throws()
    {
        // Arrange
        var sut = TicketCatalog.Create(DefaultEventId);
        var id = TicketTypeId.New();
        sut.AddTicketType(id, TicketTypeName.From("VIP"), [], 100);
        sut.CancelTicketType(id);

        // Act
        var result = ErrorResult.Capture(() =>
            sut.UpdateTicketType(id, name: null, maxCapacity: 200));

        // Assert
        result.Error.ShouldMatch(TicketCatalog.Errors.TicketTypeAlreadyCancelled(id));
    }

    [TestMethod]
    public void SC008_CancelTicketType_ActiveType_Cancels()
    {
        // Arrange
        var sut = TicketCatalog.Create(DefaultEventId);
        var id = TicketTypeId.New();
        sut.AddTicketType(id, TicketTypeName.From("VIP"), [], 100);

        // Act
        sut.CancelTicketType(id);

        // Assert
        sut.TicketTypes[0].IsCancelled.ShouldBeTrue();
    }

    [TestMethod]
    public void SC009_CancelTicketType_AlreadyCancelled_Throws()
    {
        // Arrange
        var sut = TicketCatalog.Create(DefaultEventId);
        var id = TicketTypeId.New();
        sut.AddTicketType(id, TicketTypeName.From("VIP"), [], 100);
        sut.CancelTicketType(id);

        // Act
        var result = ErrorResult.Capture(() => sut.CancelTicketType(id));

        // Assert
        result.Error.ShouldMatch(Registrations.Domain.Entities.TicketType.Errors.TicketTypeAlreadyCancelled(id));
    }

    [TestMethod]
    public void SC010_Claim_Enforce_AvailableCapacity_Increments()
    {
        // Arrange
        var sut = TicketCatalog.Create(DefaultEventId);
        var id = TicketTypeId.New();
        sut.AddTicketType(id, TicketTypeName.From("General"), [], 10);

        // Act
        sut.Claim([id], enforce: true);

        // Assert
        sut.TicketTypes[0].UsedCapacity.ShouldBe(1);
    }

    [TestMethod]
    public void SC011_Claim_Enforce_AtCapacity_Throws()
    {
        // Arrange
        var sut = TicketCatalog.Create(DefaultEventId);
        var id = TicketTypeId.New();
        sut.AddTicketType(id, TicketTypeName.From("General"), [], 1);
        sut.Claim([id], enforce: true);

        // Act
        var result = ErrorResult.Capture(() => sut.Claim([id], enforce: true));

        // Assert
        result.Error.ShouldMatch(Registrations.Domain.Entities.TicketType.Errors.TicketTypeAtCapacity(id));
    }

    [TestMethod]
    public void SC012_Claim_Enforce_NullCapacity_SelfServiceEnabled_Succeeds()
    {
        // Arrange — null capacity + self-service enabled means unlimited self-service
        var sut = TicketCatalog.Create(DefaultEventId);
        var id = TicketTypeId.New();
        sut.AddTicketType(id, TicketTypeName.From("Speaker"), [], null, selfServiceEnabled: true);

        // Act
        sut.Claim([id], enforce: true);

        // Assert
        sut.TicketTypes[0].UsedCapacity.ShouldBe(1);
    }

    [TestMethod]
    public void SC013_Claim_Uncapped_AlwaysIncrements()
    {
        // Arrange
        var sut = TicketCatalog.Create(DefaultEventId);
        var id = TicketTypeId.New();
        sut.AddTicketType(id, TicketTypeName.From("VIP"), [], 1);
        sut.Claim([id], enforce: false); // at capacity

        // Act
        sut.Claim([id], enforce: false); // should still work

        // Assert
        sut.TicketTypes[0].UsedCapacity.ShouldBe(2);
    }

    [TestMethod]
    public void SC014_Claim_MultipleIds_AllIncrement()
    {
        // Arrange
        var sut = TicketCatalog.Create(DefaultEventId);
        var idA = TicketTypeId.New();
        var idB = TicketTypeId.New();
        sut.AddTicketType(idA, TicketTypeName.From("A"), [], 10);
        sut.AddTicketType(idB, TicketTypeName.From("B"), [], 10);

        // Act
        sut.Claim([idA, idB], enforce: true);

        // Assert
        sut.TicketTypes.Single(t => t.Id == idA).UsedCapacity.ShouldBe(1);
        sut.TicketTypes.Single(t => t.Id == idB).UsedCapacity.ShouldBe(1);
    }

    [TestMethod]
    public void SC015_Claim_UnknownId_Throws()
    {
        // Arrange
        var sut = TicketCatalog.Create(DefaultEventId);
        var knownId = TicketTypeId.New();
        var unknownId = TicketTypeId.New();
        sut.AddTicketType(knownId, TicketTypeName.From("Known"), [], 10);

        // Act
        var result = ErrorResult.Capture(() => sut.Claim([unknownId], enforce: true));

        // Assert
        result.Error.ShouldMatch(TicketCatalog.Errors.UnknownTicketTypes([unknownId.Value]));
    }

    [TestMethod]
    public void SC016_GetTicketType_Exists_Returns()
    {
        // Arrange
        var sut = TicketCatalog.Create(DefaultEventId);
        var id = TicketTypeId.New();
        sut.AddTicketType(id, TicketTypeName.From("VIP"), [], 100);

        // Act
        var tt = sut.GetTicketType(id);

        // Assert
        tt.ShouldNotBeNull();
        tt.Id.ShouldBe(id);
    }

    [TestMethod]
    public void SC017_GetTicketType_NotExists_ReturnsNull()
    {
        // Arrange
        var sut = TicketCatalog.Create(DefaultEventId);
        var unknownId = TicketTypeId.New();

        // Act
        var tt = sut.GetTicketType(unknownId);

        // Assert
        tt.ShouldBeNull();
    }

    [TestMethod]
    public void SC018_NewCatalog_EventStatusIsActive()
    {
        // Arrange
        var sut = TicketCatalog.Create(DefaultEventId);

        // Assert
        sut.EventStatus.ShouldBe(EventLifecycleStatus.Active);
    }

    [TestMethod]
    public void SC019_MarkEventCancelled_FromActive_Transitions()
    {
        // Arrange
        var sut = TicketCatalog.Create(DefaultEventId);

        // Act
        sut.MarkEventCancelled();

        // Assert
        sut.EventStatus.ShouldBe(EventLifecycleStatus.Cancelled);
    }

    [TestMethod]
    public void SC020_MarkEventCancelled_AlreadyCancelled_IsIdempotent()
    {
        // Arrange
        var sut = TicketCatalog.Create(DefaultEventId);
        sut.MarkEventCancelled();

        // Act
        sut.MarkEventCancelled();

        // Assert
        sut.EventStatus.ShouldBe(EventLifecycleStatus.Cancelled);
    }

    [TestMethod]
    public void SC021_MarkEventCancelled_FromArchived_Throws()
    {
        // Arrange
        var sut = TicketCatalog.Create(DefaultEventId);
        sut.MarkEventArchived();

        // Act
        var result = ErrorResult.Capture(() => sut.MarkEventCancelled());

        // Assert
        result.Error.ShouldMatch(TicketCatalog.Errors.IllegalEventStatusTransition);
    }

    [TestMethod]
    public void SC022_MarkEventArchived_FromActive_Transitions()
    {
        // Arrange
        var sut = TicketCatalog.Create(DefaultEventId);

        // Act
        sut.MarkEventArchived();

        // Assert
        sut.EventStatus.ShouldBe(EventLifecycleStatus.Archived);
    }

    [TestMethod]
    public void SC023_MarkEventArchived_FromCancelled_Transitions()
    {
        // Arrange
        var sut = TicketCatalog.Create(DefaultEventId);
        sut.MarkEventCancelled();

        // Act
        sut.MarkEventArchived();

        // Assert
        sut.EventStatus.ShouldBe(EventLifecycleStatus.Archived);
    }

    [TestMethod]
    public void SC024_MarkEventArchived_AlreadyArchived_IsIdempotent()
    {
        // Arrange
        var sut = TicketCatalog.Create(DefaultEventId);
        sut.MarkEventArchived();

        // Act
        sut.MarkEventArchived();

        // Assert
        sut.EventStatus.ShouldBe(EventLifecycleStatus.Archived);
    }

    [TestMethod]
    public void SC025_Claim_EventCancelled_Throws()
    {
        // Arrange
        var sut = TicketCatalog.Create(DefaultEventId);
        var id = TicketTypeId.New();
        sut.AddTicketType(id, TicketTypeName.From("General"), [], 10);
        sut.MarkEventCancelled();

        // Act
        var result = ErrorResult.Capture(() => sut.Claim([id], enforce: true));

        // Assert
        result.Error.ShouldMatch(TicketCatalog.Errors.EventNotActive);
    }

    [TestMethod]
    public void SC026_Claim_EventArchived_Throws()
    {
        // Arrange
        var sut = TicketCatalog.Create(DefaultEventId);
        var id = TicketTypeId.New();
        sut.AddTicketType(id, TicketTypeName.From("General"), [], 10);
        sut.MarkEventArchived();

        // Act
        var result = ErrorResult.Capture(() => sut.Claim([id], enforce: false));

        // Assert
        result.Error.ShouldMatch(TicketCatalog.Errors.EventNotActive);
    }

    [TestMethod]
    public void SC027_AddTicketType_EventCancelled_Throws()
    {
        // Arrange
        var sut = TicketCatalog.Create(DefaultEventId);
        sut.MarkEventCancelled();

        // Act
        var result = ErrorResult.Capture(() =>
            sut.AddTicketType(TicketTypeId.New(), TicketTypeName.From("VIP"), [], 100));

        // Assert
        result.Error.ShouldMatch(TicketCatalog.Errors.EventNotActive);
    }

    [TestMethod]
    public void SC028_AddTicketType_EventArchived_Throws()
    {
        // Arrange
        var sut = TicketCatalog.Create(DefaultEventId);
        sut.MarkEventArchived();

        // Act
        var result = ErrorResult.Capture(() =>
            sut.AddTicketType(TicketTypeId.New(), TicketTypeName.From("VIP"), [], 100));

        // Assert
        result.Error.ShouldMatch(TicketCatalog.Errors.EventNotActive);
    }

    [TestMethod]
    public void SC029_UpdateTicketType_EventCancelled_Throws()
    {
        // Arrange
        var sut = TicketCatalog.Create(DefaultEventId);
        var id = TicketTypeId.New();
        sut.AddTicketType(id, TicketTypeName.From("VIP"), [], 100);
        sut.MarkEventCancelled();

        // Act
        var result = ErrorResult.Capture(() =>
            sut.UpdateTicketType(id, name: null, maxCapacity: 200));

        // Assert
        result.Error.ShouldMatch(TicketCatalog.Errors.EventNotActive);
    }

    [TestMethod]
    public void SC030_CancelTicketType_EventCancelled_Throws()
    {
        // Arrange
        var sut = TicketCatalog.Create(DefaultEventId);
        var id = TicketTypeId.New();
        sut.AddTicketType(id, TicketTypeName.From("VIP"), [], 100);
        sut.MarkEventCancelled();

        // Act
        var result = ErrorResult.Capture(() => sut.CancelTicketType(id));

        // Assert
        result.Error.ShouldMatch(TicketCatalog.Errors.EventNotActive);
    }

    [TestMethod]
    public void SC031_Release_MatchingIds_DecrementsUsedCapacity()
    {
        // Arrange
        var sut = TicketCatalog.Create(DefaultEventId);
        var id = TicketTypeId.New();
        sut.AddTicketType(id, TicketTypeName.From("General"), [], 10);
        sut.Claim([id], enforce: true);
        sut.GetTicketType(id)!.UsedCapacity.ShouldBe(1);

        // Act
        sut.Release([id]);

        // Assert
        sut.GetTicketType(id)!.UsedCapacity.ShouldBe(0);
    }

    [TestMethod]
    public void SC032_Release_UnknownId_IsSilentlySkipped()
    {
        // Arrange
        var sut = TicketCatalog.Create(DefaultEventId);
        var knownId = TicketTypeId.New();
        var unknownId = TicketTypeId.New();
        sut.AddTicketType(knownId, TicketTypeName.From("Known"), [], 10);
        sut.Claim([knownId], enforce: true);

        // Act — releasing an unknown ID should not throw
        sut.Release([unknownId, knownId]);

        // Assert — known was released; unknown was skipped without error
        sut.GetTicketType(knownId)!.UsedCapacity.ShouldBe(0);
    }

    [TestMethod]
    public void SC033_Release_MultipleIds_AllDecrement()
    {
        // Arrange
        var sut = TicketCatalog.Create(DefaultEventId);
        var idA = TicketTypeId.New();
        var idB = TicketTypeId.New();
        sut.AddTicketType(idA, TicketTypeName.From("A"), [], 10);
        sut.AddTicketType(idB, TicketTypeName.From("B"), [], 10);
        sut.Claim([idA, idB], enforce: true);

        // Act
        sut.Release([idA, idB]);

        // Assert
        sut.GetTicketType(idA)!.UsedCapacity.ShouldBe(0);
        sut.GetTicketType(idB)!.UsedCapacity.ShouldBe(0);
    }

    [TestMethod]
    public void SC034_Claim_CancelledTicketType_Throws()
    {
        // Arrange
        var sut = TicketCatalog.Create(DefaultEventId);
        var id = TicketTypeId.New();
        sut.AddTicketType(id, TicketTypeName.From("VIP"), [], 10);
        sut.CancelTicketType(id);

        // Act
        var result = ErrorResult.Capture(() => sut.Claim([id], enforce: false));

        // Assert
        result.Error.ShouldMatch(TicketCatalog.Errors.CancelledTicketTypes([id.Value]));
    }

    [TestMethod]
    public void SC035_Claim_DuplicateIds_Throws()
    {
        // Arrange
        var sut = TicketCatalog.Create(DefaultEventId);
        var id = TicketTypeId.New();
        sut.AddTicketType(id, TicketTypeName.From("General"), [], 10);

        // Act
        var result = ErrorResult.Capture(() => sut.Claim([id, id], enforce: false));

        // Assert
        result.Error.ShouldMatch(TicketCatalog.Errors.DuplicateTicketTypes([id.Value]));
    }

    [TestMethod]
    public void SC036_Claim_OverlappingTimeSlots_Throws()
    {
        // Arrange
        var sut = TicketCatalog.Create(DefaultEventId);
        var idA = TicketTypeId.New();
        var idB = TicketTypeId.New();
        sut.AddTicketType(idA, TicketTypeName.From("Workshop A"),
            [TimeSlot.From("morning")], 10);
        sut.AddTicketType(idB, TicketTypeName.From("Workshop B"),
            [TimeSlot.From("morning")], 10);

        // Act
        var result = ErrorResult.Capture(() => sut.Claim([idA, idB], enforce: false));

        // Assert
        result.Error.ShouldMatch(TicketCatalog.Errors.OverlappingTimeSlots(["morning"]));
    }

    [TestMethod]
    public void SC037_Claim_EmptyList_IsNoOp()
    {
        // Arrange
        var sut = TicketCatalog.Create(DefaultEventId);
        var id = TicketTypeId.New();
        sut.AddTicketType(id, TicketTypeName.From("General"), [], 10);

        // Act — empty claim should not throw
        sut.Claim([], enforce: true);

        // Assert — capacity unchanged
        sut.GetTicketType(id)!.UsedCapacity.ShouldBe(0);
    }

    [TestMethod]
    public void SC038_Claim_Enforce_NonSelfServiceTicketType_Throws()
    {
        // Arrange
        var sut = TicketCatalog.Create(DefaultEventId);
        var id = TicketTypeId.New();
        sut.AddTicketType(id, TicketTypeName.From("VIP"), [], 50, selfServiceEnabled: false);

        // Act
        var result = ErrorResult.Capture(() => sut.Claim([id], enforce: true));

        // Assert
        result.Error.ShouldMatch(TicketCatalog.Errors.TicketTypesNotSelfService([id.Value]));
    }

    [TestMethod]
    public void SC039_Claim_NoEnforce_NonSelfServiceTicketType_Succeeds()
    {
        // Arrange — admin/coupon bypass: enforce=false skips self-service check
        var sut = TicketCatalog.Create(DefaultEventId);
        var id = TicketTypeId.New();
        sut.AddTicketType(id, TicketTypeName.From("VIP"), [], 50, selfServiceEnabled: false);

        // Act
        sut.Claim([id], enforce: false);

        // Assert
        sut.GetTicketType(id)!.UsedCapacity.ShouldBe(1);
    }
}
