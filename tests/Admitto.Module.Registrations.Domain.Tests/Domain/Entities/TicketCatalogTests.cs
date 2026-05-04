using Amolenk.Admitto.Module.Registrations.Domain.Entities;
using Amolenk.Admitto.Module.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;
using Amolenk.Admitto.Testing.Infrastructure.Assertions;
using Shouldly;

namespace Amolenk.Admitto.Module.Registrations.Domain.Tests.Entities;

[TestClass]
public sealed class TicketCatalogTests
{
    private static readonly TicketedEventId DefaultEventId = TicketedEventId.New();

    [TestMethod]
    public void SC001_AddTicketType_ActiveEvent_AddsSuccessfully()
    {
        // Arrange
        var sut = TicketCatalog.Create(DefaultEventId);

        // Act
        sut.AddTicketType(
            Slug.From("vip"),
            DisplayName.From("VIP Pass"),
            [new TimeSlot(Slug.From("morning"))],
            100);

        // Assert
        sut.TicketTypes.Count.ShouldBe(1);
        var tt = sut.TicketTypes[0];
        tt.Id.ShouldBe("vip");
        tt.Name.ShouldBe(DisplayName.From("VIP Pass"));
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
            Slug.From("speaker"),
            DisplayName.From("Speaker Pass"),
            [],
            maxCapacity: null);

        // Assert
        sut.TicketTypes[0].MaxCapacity.ShouldBeNull();
    }

    [TestMethod]
    public void SC003_AddTicketType_DuplicateSlug_Throws()
    {
        // Arrange
        var sut = TicketCatalog.Create(DefaultEventId);
        sut.AddTicketType(Slug.From("vip"), DisplayName.From("VIP"), [], 100);

        // Act
        var result = ErrorResult.Capture(() =>
            sut.AddTicketType(Slug.From("vip"), DisplayName.From("VIP 2"), [], 50));

        // Assert
        result.Error.ShouldMatch(TicketCatalog.Errors.DuplicateTicketTypeSlug(Slug.From("vip")));
    }

    [TestMethod]
    public void SC004_UpdateTicketType_Capacity_Updates()
    {
        // Arrange
        var sut = TicketCatalog.Create(DefaultEventId);
        sut.AddTicketType(Slug.From("vip"), DisplayName.From("VIP"), [], 100);

        // Act
        sut.UpdateTicketType(Slug.From("vip"), name: null, maxCapacity: 200);

        // Assert
        sut.TicketTypes[0].MaxCapacity.ShouldBe(200);
    }

    [TestMethod]
    public void SC005_UpdateTicketType_Name_Updates()
    {
        // Arrange
        var sut = TicketCatalog.Create(DefaultEventId);
        sut.AddTicketType(Slug.From("vip"), DisplayName.From("VIP"), [], 100);

        // Act
        sut.UpdateTicketType(Slug.From("vip"), name: DisplayName.From("VIP Access"), maxCapacity: 100);

        // Assert
        sut.TicketTypes[0].Name.ShouldBe(DisplayName.From("VIP Access"));
    }

    [TestMethod]
    public void SC006_UpdateTicketType_NotFound_Throws()
    {
        // Arrange
        var sut = TicketCatalog.Create(DefaultEventId);

        // Act
        var result = ErrorResult.Capture(() =>
            sut.UpdateTicketType(Slug.From("nonexistent"), name: null, maxCapacity: 100));

        // Assert
        result.Error.ShouldMatch(TicketCatalog.Errors.TicketTypeNotFound("nonexistent"));
    }

    [TestMethod]
    public void SC007_UpdateTicketType_CancelledType_Throws()
    {
        // Arrange
        var sut = TicketCatalog.Create(DefaultEventId);
        sut.AddTicketType(Slug.From("vip"), DisplayName.From("VIP"), [], 100);
        sut.CancelTicketType(Slug.From("vip"));

        // Act
        var result = ErrorResult.Capture(() =>
            sut.UpdateTicketType(Slug.From("vip"), name: null, maxCapacity: 200));

        // Assert
        result.Error.ShouldMatch(TicketCatalog.Errors.TicketTypeAlreadyCancelled(Slug.From("vip")));
    }

    [TestMethod]
    public void SC008_CancelTicketType_ActiveType_Cancels()
    {
        // Arrange
        var sut = TicketCatalog.Create(DefaultEventId);
        sut.AddTicketType(Slug.From("vip"), DisplayName.From("VIP"), [], 100);

        // Act
        sut.CancelTicketType(Slug.From("vip"));

        // Assert
        sut.TicketTypes[0].IsCancelled.ShouldBeTrue();
    }

    [TestMethod]
    public void SC009_CancelTicketType_AlreadyCancelled_Throws()
    {
        // Arrange
        var sut = TicketCatalog.Create(DefaultEventId);
        sut.AddTicketType(Slug.From("vip"), DisplayName.From("VIP"), [], 100);
        sut.CancelTicketType(Slug.From("vip"));

        // Act
        var result = ErrorResult.Capture(() => sut.CancelTicketType(Slug.From("vip")));

        // Assert
        result.Error.ShouldMatch(Registrations.Domain.Entities.TicketType.Errors.TicketTypeAlreadyCancelled("vip"));
    }

    [TestMethod]
    public void SC010_Claim_Enforce_AvailableCapacity_Increments()
    {
        // Arrange
        var sut = TicketCatalog.Create(DefaultEventId);
        sut.AddTicketType(Slug.From("general"), DisplayName.From("General"), [], 10);

        // Act
        sut.Claim(["general"], enforce: true);

        // Assert
        sut.TicketTypes[0].UsedCapacity.ShouldBe(1);
    }

    [TestMethod]
    public void SC011_Claim_Enforce_AtCapacity_Throws()
    {
        // Arrange
        var sut = TicketCatalog.Create(DefaultEventId);
        sut.AddTicketType(Slug.From("general"), DisplayName.From("General"), [], 1);
        sut.Claim(["general"], enforce: true);

        // Act
        var result = ErrorResult.Capture(() => sut.Claim(["general"], enforce: true));

        // Assert
        result.Error.ShouldMatch(Registrations.Domain.Entities.TicketType.Errors.TicketTypeAtCapacity("general"));
    }

    [TestMethod]
    public void SC012_Claim_Enforce_NullCapacity_SelfServiceEnabled_Succeeds()
    {
        // Arrange — null capacity + self-service enabled means unlimited self-service
        var sut = TicketCatalog.Create(DefaultEventId);
        sut.AddTicketType(Slug.From("speaker"), DisplayName.From("Speaker"), [], null, selfServiceEnabled: true);

        // Act
        sut.Claim(["speaker"], enforce: true);

        // Assert
        sut.TicketTypes[0].UsedCapacity.ShouldBe(1);
    }

    [TestMethod]
    public void SC013_Claim_Uncapped_AlwaysIncrements()
    {
        // Arrange
        var sut = TicketCatalog.Create(DefaultEventId);
        sut.AddTicketType(Slug.From("vip"), DisplayName.From("VIP"), [], 1);
        sut.Claim(["vip"], enforce: false); // at capacity

        // Act
        sut.Claim(["vip"], enforce: false); // should still work

        // Assert
        sut.TicketTypes[0].UsedCapacity.ShouldBe(2);
    }

    [TestMethod]
    public void SC014_Claim_MultipleSlugs_AllIncrement()
    {
        // Arrange
        var sut = TicketCatalog.Create(DefaultEventId);
        sut.AddTicketType(Slug.From("a"), DisplayName.From("A"), [], 10);
        sut.AddTicketType(Slug.From("b"), DisplayName.From("B"), [], 10);

        // Act
        sut.Claim(["a", "b"], enforce: true);

        // Assert
        sut.TicketTypes.Single(t => t.Id == "a").UsedCapacity.ShouldBe(1);
        sut.TicketTypes.Single(t => t.Id == "b").UsedCapacity.ShouldBe(1);
    }

    [TestMethod]
    public void SC015_Claim_UnknownSlug_Throws()
    {
        // Arrange
        var sut = TicketCatalog.Create(DefaultEventId);
        sut.AddTicketType(Slug.From("known"), DisplayName.From("Known"), [], 10);

        // Act
        var result = ErrorResult.Capture(() => sut.Claim(["unknown"], enforce: true));

        // Assert
        result.Error.ShouldMatch(TicketCatalog.Errors.UnknownTicketTypes(["unknown"]));
    }

    [TestMethod]
    public void SC016_GetTicketType_Exists_Returns()
    {
        // Arrange
        var sut = TicketCatalog.Create(DefaultEventId);
        sut.AddTicketType(Slug.From("vip"), DisplayName.From("VIP"), [], 100);

        // Act
        var tt = sut.GetTicketType("vip");

        // Assert
        tt.ShouldNotBeNull();
        tt.Id.ShouldBe("vip");
    }

    [TestMethod]
    public void SC017_GetTicketType_NotExists_ReturnsNull()
    {
        // Arrange
        var sut = TicketCatalog.Create(DefaultEventId);

        // Act
        var tt = sut.GetTicketType("nonexistent");

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
        sut.AddTicketType(Slug.From("general"), DisplayName.From("General"), [], 10);
        sut.MarkEventCancelled();

        // Act
        var result = ErrorResult.Capture(() => sut.Claim(["general"], enforce: true));

        // Assert
        result.Error.ShouldMatch(TicketCatalog.Errors.EventNotActive);
    }

    [TestMethod]
    public void SC026_Claim_EventArchived_Throws()
    {
        // Arrange
        var sut = TicketCatalog.Create(DefaultEventId);
        sut.AddTicketType(Slug.From("general"), DisplayName.From("General"), [], 10);
        sut.MarkEventArchived();

        // Act
        var result = ErrorResult.Capture(() => sut.Claim(["general"], enforce: false));

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
            sut.AddTicketType(Slug.From("vip"), DisplayName.From("VIP"), [], 100));

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
            sut.AddTicketType(Slug.From("vip"), DisplayName.From("VIP"), [], 100));

        // Assert
        result.Error.ShouldMatch(TicketCatalog.Errors.EventNotActive);
    }

    [TestMethod]
    public void SC029_UpdateTicketType_EventCancelled_Throws()
    {
        // Arrange
        var sut = TicketCatalog.Create(DefaultEventId);
        sut.AddTicketType(Slug.From("vip"), DisplayName.From("VIP"), [], 100);
        sut.MarkEventCancelled();

        // Act
        var result = ErrorResult.Capture(() =>
            sut.UpdateTicketType(Slug.From("vip"), name: null, maxCapacity: 200));

        // Assert
        result.Error.ShouldMatch(TicketCatalog.Errors.EventNotActive);
    }

    [TestMethod]
    public void SC030_CancelTicketType_EventCancelled_Throws()
    {
        // Arrange
        var sut = TicketCatalog.Create(DefaultEventId);
        sut.AddTicketType(Slug.From("vip"), DisplayName.From("VIP"), [], 100);
        sut.MarkEventCancelled();

        // Act
        var result = ErrorResult.Capture(() => sut.CancelTicketType(Slug.From("vip")));

        // Assert
        result.Error.ShouldMatch(TicketCatalog.Errors.EventNotActive);
    }

    [TestMethod]
    public void SC031_Release_MatchingSlugs_DecrementsUsedCapacity()
    {
        // Arrange
        var sut = TicketCatalog.Create(DefaultEventId);
        sut.AddTicketType(Slug.From("general"), DisplayName.From("General"), [], 10);
        sut.Claim(["general"], enforce: true);
        sut.GetTicketType("general")!.UsedCapacity.ShouldBe(1);

        // Act
        sut.Release(["general"]);

        // Assert
        sut.GetTicketType("general")!.UsedCapacity.ShouldBe(0);
    }

    [TestMethod]
    public void SC032_Release_UnknownSlug_IsSilentlySkipped()
    {
        // Arrange
        var sut = TicketCatalog.Create(DefaultEventId);
        sut.AddTicketType(Slug.From("known"), DisplayName.From("Known"), [], 10);
        sut.Claim(["known"], enforce: true);

        // Act — releasing an unknown slug should not throw
        sut.Release(["unknown", "known"]);

        // Assert — known slug was released; unknown was skipped without error
        sut.GetTicketType("known")!.UsedCapacity.ShouldBe(0);
    }

    [TestMethod]
    public void SC033_Release_MultipleSlugs_AllDecrement()
    {
        // Arrange
        var sut = TicketCatalog.Create(DefaultEventId);
        sut.AddTicketType(Slug.From("a"), DisplayName.From("A"), [], 10);
        sut.AddTicketType(Slug.From("b"), DisplayName.From("B"), [], 10);
        sut.Claim(["a", "b"], enforce: true);

        // Act
        sut.Release(["a", "b"]);

        // Assert
        sut.GetTicketType("a")!.UsedCapacity.ShouldBe(0);
        sut.GetTicketType("b")!.UsedCapacity.ShouldBe(0);
    }

    [TestMethod]
    public void SC034_Claim_CancelledTicketType_Throws()
    {
        // Arrange
        var sut = TicketCatalog.Create(DefaultEventId);
        sut.AddTicketType(Slug.From("vip"), DisplayName.From("VIP"), [], 10);
        sut.CancelTicketType(Slug.From("vip"));

        // Act
        var result = ErrorResult.Capture(() => sut.Claim(["vip"], enforce: false));

        // Assert
        result.Error.ShouldMatch(TicketCatalog.Errors.CancelledTicketTypes(["vip"]));
    }

    [TestMethod]
    public void SC035_Claim_DuplicateSlugs_Throws()
    {
        // Arrange
        var sut = TicketCatalog.Create(DefaultEventId);
        sut.AddTicketType(Slug.From("general"), DisplayName.From("General"), [], 10);

        // Act
        var result = ErrorResult.Capture(() => sut.Claim(["general", "general"], enforce: false));

        // Assert
        result.Error.ShouldMatch(TicketCatalog.Errors.DuplicateTicketTypes(["general"]));
    }

    [TestMethod]
    public void SC036_Claim_OverlappingTimeSlots_Throws()
    {
        // Arrange
        var sut = TicketCatalog.Create(DefaultEventId);
        sut.AddTicketType(Slug.From("workshop-a"), DisplayName.From("Workshop A"),
            [new TimeSlot(Slug.From("morning"))], 10);
        sut.AddTicketType(Slug.From("workshop-b"), DisplayName.From("Workshop B"),
            [new TimeSlot(Slug.From("morning"))], 10);

        // Act
        var result = ErrorResult.Capture(() => sut.Claim(["workshop-a", "workshop-b"], enforce: false));

        // Assert
        result.Error.ShouldMatch(TicketCatalog.Errors.OverlappingTimeSlots(["morning"]));
    }

    [TestMethod]
    public void SC037_Claim_EmptyList_IsNoOp()
    {
        // Arrange
        var sut = TicketCatalog.Create(DefaultEventId);
        sut.AddTicketType(Slug.From("general"), DisplayName.From("General"), [], 10);

        // Act — empty claim should not throw
        sut.Claim([], enforce: true);

        // Assert — capacity unchanged
        sut.GetTicketType("general")!.UsedCapacity.ShouldBe(0);
    }

    [TestMethod]
    public void SC038_Claim_Enforce_NonSelfServiceTicketType_Throws()
    {
        // Arrange
        var sut = TicketCatalog.Create(DefaultEventId);
        sut.AddTicketType(Slug.From("vip"), DisplayName.From("VIP"), [], 50, selfServiceEnabled: false);

        // Act
        var result = ErrorResult.Capture(() => sut.Claim(["vip"], enforce: true));

        // Assert
        result.Error.ShouldMatch(TicketCatalog.Errors.TicketTypesNotSelfService(["vip"]));
    }

    [TestMethod]
    public void SC039_Claim_NoEnforce_NonSelfServiceTicketType_Succeeds()
    {
        // Arrange — admin/coupon bypass: enforce=false skips self-service check
        var sut = TicketCatalog.Create(DefaultEventId);
        sut.AddTicketType(Slug.From("vip"), DisplayName.From("VIP"), [], 50, selfServiceEnabled: false);

        // Act
        sut.Claim(["vip"], enforce: false);

        // Assert
        sut.GetTicketType("vip")!.UsedCapacity.ShouldBe(1);
    }
}
