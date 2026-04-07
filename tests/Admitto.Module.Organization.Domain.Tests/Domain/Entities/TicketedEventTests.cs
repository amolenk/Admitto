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
    public void SC009_Cancel_ActiveEvent_CancelsEventAndRaisesDomainEvent()
    {
        // Arrange
        var sut = new TicketedEventBuilder()
            .AsActive()
            .Build();

        // Act
        sut.Cancel();

        // Assert
        sut.Status.ShouldBe(EventStatus.Cancelled);
        sut.GetDomainEvents()
            .OfType<TicketedEventCancelledDomainEvent>()
            .ShouldHaveSingleItem()
            .TicketedEventId.ShouldBe(sut.Id);
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
    public void SC011_Archive_ActiveEvent_ChangesStatusToArchivedAndRaisesDomainEvent()
    {
        // Arrange
        var sut = new TicketedEventBuilder().AsActive().Build();

        // Act
        sut.Archive();

        // Assert
        sut.Status.ShouldBe(EventStatus.Archived);
        sut.GetDomainEvents()
            .OfType<TicketedEventArchivedDomainEvent>()
            .ShouldHaveSingleItem()
            .TicketedEventId.ShouldBe(sut.Id);
    }

    [TestMethod]
    public void SC012_Archive_CancelledEvent_ChangesStatusToArchivedAndRaisesDomainEvent()
    {
        // Arrange
        var sut = new TicketedEventBuilder().AsCancelled().Build();

        // Act
        sut.Archive();

        // Assert
        sut.Status.ShouldBe(EventStatus.Archived);
        sut.GetDomainEvents()
            .OfType<TicketedEventArchivedDomainEvent>()
            .ShouldHaveSingleItem()
            .TicketedEventId.ShouldBe(sut.Id);
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
}
