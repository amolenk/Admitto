using Amolenk.Admitto.Module.Organization.Domain.DomainEvents;
using Amolenk.Admitto.Module.Organization.Domain.Entities;
using Amolenk.Admitto.Module.Organization.Domain.Tests.Builders;
using Amolenk.Admitto.Module.Organization.Domain.ValueObjects;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;
using Amolenk.Admitto.Testing.Infrastructure.Assertions;
using Shouldly;

namespace Amolenk.Admitto.Module.Organization.Domain.Tests.Entities;

[TestClass]
public sealed class TicketedEventTests
{
    // -------------------------------------------------------------------------
    // Cancel()
    // -------------------------------------------------------------------------

    [TestMethod]
    public void SC009_Cancel_ActiveEvent_CancelsEventAndTicketTypes()
    {
        // Arrange
        var sut = new TicketedEventBuilder()
            .AsActive()
            .WithTicketType("general-admission", "General Admission")
            .WithTicketType("vip", "VIP")
            .Build();

        // Act
        sut.Cancel();

        // Assert
        sut.Status.ShouldBe(EventStatus.Cancelled);
        sut.TicketTypes.ShouldAllBe(tt => tt.IsCancelled);
    }

    [TestMethod]
    public void SC010_Cancel_AlreadyCancelledEvent_ThrowsAlreadyCancelled()
    {
        // Arrange
        var sut = new TicketedEventBuilder().AsCancelled().Build();

        // Act
        var result = ErrorResult.Capture(() => sut.Cancel());

        // Assert
        result.Error.ShouldMatch(TicketedEvent.Errors.EventAlreadyCancelled(sut.Id));
    }

    [TestMethod]
    public void Cancel_ArchivedEvent_ThrowsEventArchived()
    {
        // Arrange
        var sut = new TicketedEventBuilder().AsArchived().Build();

        // Act
        var result = ErrorResult.Capture(() => sut.Cancel());

        // Assert
        result.Error.ShouldMatch(TicketedEvent.Errors.EventArchived(sut.Id));
    }

    // -------------------------------------------------------------------------
    // Archive()
    // -------------------------------------------------------------------------

    [TestMethod]
    public void SC011_Archive_ActiveEvent_ChangesStatusToArchived()
    {
        // Arrange
        var sut = new TicketedEventBuilder().AsActive().Build();

        // Act
        sut.Archive();

        // Assert
        sut.Status.ShouldBe(EventStatus.Archived);
    }

    [TestMethod]
    public void SC012_Archive_CancelledEvent_ChangesStatusToArchived()
    {
        // Arrange
        var sut = new TicketedEventBuilder().AsCancelled().Build();

        // Act
        sut.Archive();

        // Assert
        sut.Status.ShouldBe(EventStatus.Archived);
    }

    [TestMethod]
    public void SC013_Archive_AlreadyArchivedEvent_ThrowsAlreadyArchived()
    {
        // Arrange
        var sut = new TicketedEventBuilder().AsArchived().Build();

        // Act
        var result = ErrorResult.Capture(() => sut.Archive());

        // Assert
        result.Error.ShouldMatch(TicketedEvent.Errors.EventAlreadyArchived(sut.Id));
    }

    // -------------------------------------------------------------------------
    // Update()
    // -------------------------------------------------------------------------

    [TestMethod]
    public void Update_CancelledEvent_ThrowsEventCancelled()
    {
        // Arrange
        var sut = new TicketedEventBuilder().AsCancelled().Build();

        // Act
        var result = ErrorResult.Capture(() =>
            sut.Update(DisplayName.From("New Name"), null, null, null));

        // Assert
        result.Error.ShouldMatch(TicketedEvent.Errors.EventCancelled(sut.Id));
    }

    [TestMethod]
    public void Update_ArchivedEvent_ThrowsEventArchived()
    {
        // Arrange
        var sut = new TicketedEventBuilder().AsArchived().Build();

        // Act
        var result = ErrorResult.Capture(() =>
            sut.Update(DisplayName.From("New Name"), null, null, null));

        // Assert
        result.Error.ShouldMatch(TicketedEvent.Errors.EventArchived(sut.Id));
    }

    // -------------------------------------------------------------------------
    // AddTicketType()
    // -------------------------------------------------------------------------

    [TestMethod]
    public void SC014_AddTicketType_ActiveEvent_AddsTicketTypeAndRaisesDomainEvent()
    {
        // Arrange
        var sut = new TicketedEventBuilder().AsActive().Build();
        var slug = Slug.From("general-admission");
        var name = DisplayName.From("General Admission");
        var capacity = Capacity.From(100);

        // Act
        sut.AddTicketType(slug, name, timeSlots: [new TimeSlot(Slug.From("morning"))], capacity: capacity);

        // Assert
        sut.TicketTypes.ShouldHaveSingleItem().ShouldSatisfyAllConditions(tt =>
        {
            tt.Slug.ShouldBe(slug);
            tt.Name.ShouldBe(name);
            tt.Capacity.ShouldBe(capacity);
            tt.IsCancelled.ShouldBeFalse();
        });

        sut.GetDomainEvents()
            .OfType<TicketTypeAddedDomainEvent>()
            .ShouldHaveSingleItem()
            .ShouldSatisfyAllConditions(e =>
            {
                e.TicketedEventId.ShouldBe(sut.Id);
                e.Slug.ShouldBe("general-admission");
                e.Name.ShouldBe("General Admission");
                e.TimeSlots.ShouldBe(["morning"]);
                e.Capacity.ShouldBe(100);
            });
    }

    [TestMethod]
    public void SC014b_AddTicketType_NullCapacity_RaisesDomainEventWithNullCapacity()
    {
        // Arrange
        var sut = new TicketedEventBuilder().AsActive().Build();

        // Act
        sut.AddTicketType(Slug.From("speaker"), DisplayName.From("Speaker Pass"), timeSlots: [], capacity: null);

        // Assert
        sut.TicketTypes.ShouldHaveSingleItem().Capacity.ShouldBeNull();

        sut.GetDomainEvents()
            .OfType<TicketTypeAddedDomainEvent>()
            .ShouldHaveSingleItem()
            .ShouldSatisfyAllConditions(e =>
            {
                e.Slug.ShouldBe("speaker");
                e.Capacity.ShouldBeNull();
            });
    }

    [TestMethod]
    public void SC015_AddTicketType_DuplicateSlug_ThrowsDuplicateTicketTypeSlug()
    {
        // Arrange
        var slug = Slug.From("general-admission");

        var sut = new TicketedEventBuilder()
            .AsActive()
            .WithTicketType("general-admission", "General Admission")
            .Build();

        // Act
        var result = ErrorResult.Capture(() =>
            sut.AddTicketType(slug, DisplayName.From("Duplicate"), [], null));

        // Assert
        result.Error.ShouldMatch(TicketedEvent.Errors.DuplicateTicketTypeSlug(slug));
    }

    [TestMethod]
    public void SC016_AddTicketType_CancelledEvent_ThrowsEventCancelled()
    {
        // Arrange
        var sut = new TicketedEventBuilder().AsCancelled().Build();

        // Act
        var result = ErrorResult.Capture(() =>
            sut.AddTicketType(Slug.From("new-type"), DisplayName.From("New Type"), [], null));

        // Assert
        result.Error.ShouldMatch(TicketedEvent.Errors.EventCancelled(sut.Id));
    }

    // -------------------------------------------------------------------------
    // UpdateTicketType()
    // -------------------------------------------------------------------------

    [TestMethod]
    public void SC017_UpdateTicketType_UpdatesCapacityAndRaisesDomainEvent()
    {
        // Arrange
        var slug = Slug.From("general-admission");

        var sut = new TicketedEventBuilder()
            .AsActive()
            .WithTicketType("general-admission", "General Admission")
            .Build();

        var newCapacity = Capacity.From(100);

        // Act
        sut.UpdateTicketType(slug, null, newCapacity);

        // Assert
        sut.TicketTypes.ShouldHaveSingleItem().ShouldSatisfyAllConditions(tt =>
        {
            tt.Capacity.ShouldBe(newCapacity);
        });

        sut.GetDomainEvents()
            .OfType<TicketTypeCapacityChangedDomainEvent>()
            .ShouldHaveSingleItem()
            .ShouldSatisfyAllConditions(e =>
            {
                e.TicketedEventId.ShouldBe(sut.Id);
                e.Slug.ShouldBe("general-admission");
                e.Capacity.ShouldBe(100);
            });
    }

    [TestMethod]
    public void SC017b_UpdateTicketType_NameOnly_DoesNotRaiseCapacityChangedDomainEvent()
    {
        // Arrange
        var slug = Slug.From("general-admission");

        var sut = new TicketedEventBuilder()
            .AsActive()
            .WithTicketType("general-admission", "General Admission", capacity: 200)
            .Build();

        // Clear any domain events from AddTicketType.
        sut.ClearDomainEvents();

        // Act — update name only, keep same capacity
        sut.UpdateTicketType(slug, DisplayName.From("GA Pass"), Capacity.From(200));

        // Assert
        sut.TicketTypes.ShouldHaveSingleItem().Name.ShouldBe(DisplayName.From("GA Pass"));
        sut.GetDomainEvents().OfType<TicketTypeCapacityChangedDomainEvent>().ShouldBeEmpty();
    }

    [TestMethod]
    public void UpdateTicketType_CancelledTicketType_ThrowsTicketTypeAlreadyCancelled()
    {
        // Arrange
        var slug = Slug.From("general-admission");

        var sut = new TicketedEventBuilder()
            .AsActive()
            .WithTicketType("general-admission", "General Admission")
            .Build();

        sut.CancelTicketType(slug);

        // Act
        var result = ErrorResult.Capture(() =>
            sut.UpdateTicketType(slug, null, Capacity.From(50)));

        // Assert
        result.Error.ShouldMatch(TicketedEvent.Errors.TicketTypeAlreadyCancelled(slug));
    }

    // -------------------------------------------------------------------------
    // CancelTicketType()
    // -------------------------------------------------------------------------

    [TestMethod]
    public void SC019_CancelTicketType_ActiveTicketType_CancelsTicketType()
    {
        // Arrange
        var slug = Slug.From("general-admission");

        var sut = new TicketedEventBuilder()
            .AsActive()
            .WithTicketType("general-admission", "General Admission")
            .Build();

        // Act
        sut.CancelTicketType(slug);

        // Assert
        sut.TicketTypes.ShouldHaveSingleItem().IsCancelled.ShouldBeTrue();
    }

    [TestMethod]
    public void SC020_CancelTicketType_AlreadyCancelledTicketType_ThrowsAlreadyCancelled()
    {
        // Arrange
        var slug = Slug.From("general-admission");

        var sut = new TicketedEventBuilder()
            .AsActive()
            .WithTicketType("general-admission", "General Admission")
            .Build();

        sut.CancelTicketType(slug);

        // Act
        var result = ErrorResult.Capture(() => sut.CancelTicketType(slug));

        // Assert
        result.Error.ShouldMatch(TicketedEvent.Errors.TicketTypeAlreadyCancelled(slug));
    }
}
