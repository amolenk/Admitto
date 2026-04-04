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
    public void SC014_AddTicketType_ActiveEvent_AddsTicketType()
    {
        // Arrange
        var sut = new TicketedEventBuilder().AsActive().Build();
        var slug = Slug.From("general-admission");
        var name = DisplayName.From("General Admission");

        // Act
        sut.AddTicketType(slug, name, timeSlots: [], capacity: null);

        // Assert
        sut.TicketTypes.ShouldHaveSingleItem().ShouldSatisfyAllConditions(tt =>
        {
            tt.Slug.ShouldBe(slug);
            tt.Name.ShouldBe(name);
            tt.IsCancelled.ShouldBeFalse();
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
    public void SC017_UpdateTicketType_UpdatesCapacity()
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
