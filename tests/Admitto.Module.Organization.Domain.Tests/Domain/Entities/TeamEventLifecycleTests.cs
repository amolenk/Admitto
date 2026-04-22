using Amolenk.Admitto.Module.Organization.Domain.Entities;
using Amolenk.Admitto.Module.Organization.Domain.Tests.Builders;
using Amolenk.Admitto.Module.Organization.Domain.ValueObjects;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;
using Amolenk.Admitto.Testing.Infrastructure.Assertions;
using Shouldly;

namespace Amolenk.Admitto.Module.Organization.Domain.Tests.Entities;

/// <summary>
/// Tests for the event-lifecycle counters and <see cref="TeamEventCreationRequest"/>
/// transitions on <see cref="Team"/>.
/// </summary>
[TestClass]
public sealed class TeamEventLifecycleTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.Parse("2026-01-01T00:00:00Z");
    private static readonly UserId Requester = UserId.New();

    // -------------------------------------------------------------------------
    // RequestEventCreation
    // -------------------------------------------------------------------------

    [TestMethod]
    public void RequestEventCreation_ActiveTeam_AddsPendingRequestAndIncrementsPendingCounter()
    {
        // Arrange
        var sut = new TeamBuilder().Build();
        var slug = Slug.From("my-event");

        // Act
        var request = sut.RequestEventCreation(slug, Requester, Now);

        // Assert
        sut.PendingEventCount.ShouldBe(1);
        sut.ActiveEventCount.ShouldBe(0);
        sut.EventCreationRequests.ShouldContain(request);
        request.RequestedSlug.ShouldBe(slug);
        request.RequesterId.ShouldBe(Requester);
        request.RequestedAt.ShouldBe(Now);
        request.Status.ShouldBe(TeamEventCreationRequestStatus.Pending);
    }

    [TestMethod]
    public void RequestEventCreation_ArchivedTeam_ThrowsTeamArchived()
    {
        // Arrange
        var sut = new TeamBuilder().AsArchived().Build();

        // Act
        var result = ErrorResult.Capture(() =>
            sut.RequestEventCreation(Slug.From("my-event"), Requester, Now));

        // Assert
        result.Error.ShouldMatch(Team.Errors.TeamArchived(sut.Id));
    }

    // -------------------------------------------------------------------------
    // RegisterEventCreated
    // -------------------------------------------------------------------------

    [TestMethod]
    public void RegisterEventCreated_PendingRequest_TransitionsAndSwapsCounters()
    {
        // Arrange
        var sut = new TeamBuilder().Build();
        var request = sut.RequestEventCreation(Slug.From("e1"), Requester, Now);
        var eventId = TicketedEventId.New();

        // Act
        sut.RegisterEventCreated(request.Id, eventId, Now.AddMinutes(1));

        // Assert
        sut.PendingEventCount.ShouldBe(0);
        sut.ActiveEventCount.ShouldBe(1);
        request.Status.ShouldBe(TeamEventCreationRequestStatus.Created);
        request.TicketedEventId.ShouldBe(eventId);
        request.ObservedEventStatus.ShouldBe(EventStatus.Active);
    }

    [TestMethod]
    public void RegisterEventCreated_AlreadyTerminal_IsIdempotent()
    {
        // Arrange
        var sut = new TeamBuilder().Build();
        var request = sut.RequestEventCreation(Slug.From("e1"), Requester, Now);
        var eventId = TicketedEventId.New();
        sut.RegisterEventCreated(request.Id, eventId, Now);

        // Act — redeliver the same event
        sut.RegisterEventCreated(request.Id, eventId, Now);

        // Assert — counters unchanged
        sut.PendingEventCount.ShouldBe(0);
        sut.ActiveEventCount.ShouldBe(1);
    }

    [TestMethod]
    public void RegisterEventCreated_UnknownRequestId_IsNoOp()
    {
        // Arrange
        var sut = new TeamBuilder().Build();
        sut.RequestEventCreation(Slug.From("e1"), Requester, Now);

        // Act
        sut.RegisterEventCreated(CreationRequestId.New(), TicketedEventId.New(), Now);

        // Assert
        sut.PendingEventCount.ShouldBe(1);
        sut.ActiveEventCount.ShouldBe(0);
    }

    // -------------------------------------------------------------------------
    // RegisterEventCreationRejected
    // -------------------------------------------------------------------------

    [TestMethod]
    public void RegisterEventCreationRejected_PendingRequest_TransitionsAndDecrementsPending()
    {
        // Arrange
        var sut = new TeamBuilder().Build();
        var request = sut.RequestEventCreation(Slug.From("e1"), Requester, Now);

        // Act
        sut.RegisterEventCreationRejected(request.Id, "duplicate_slug", Now.AddSeconds(2));

        // Assert
        sut.PendingEventCount.ShouldBe(0);
        sut.ActiveEventCount.ShouldBe(0);
        request.Status.ShouldBe(TeamEventCreationRequestStatus.Rejected);
        request.RejectionReason.ShouldBe("duplicate_slug");
    }

    [TestMethod]
    public void RegisterEventCreationRejected_AlreadyTerminal_IsIdempotent()
    {
        // Arrange
        var sut = new TeamBuilder().Build();
        var request = sut.RequestEventCreation(Slug.From("e1"), Requester, Now);
        sut.RegisterEventCreationRejected(request.Id, "duplicate_slug", Now);

        // Act
        sut.RegisterEventCreationRejected(request.Id, "duplicate_slug", Now);

        // Assert
        sut.PendingEventCount.ShouldBe(0);
    }

    // -------------------------------------------------------------------------
    // ExpireEventCreationRequest
    // -------------------------------------------------------------------------

    [TestMethod]
    public void ExpireEventCreationRequest_PendingRequest_TransitionsAndDecrementsPending()
    {
        // Arrange
        var sut = new TeamBuilder().Build();
        var request = sut.RequestEventCreation(Slug.From("e1"), Requester, Now);

        // Act
        sut.ExpireEventCreationRequest(request.Id, Now.AddHours(25));

        // Assert
        sut.PendingEventCount.ShouldBe(0);
        request.Status.ShouldBe(TeamEventCreationRequestStatus.Expired);
    }

    [TestMethod]
    public void ExpireEventCreationRequest_AlreadyTerminal_IsIdempotent()
    {
        // Arrange
        var sut = new TeamBuilder().Build();
        var request = sut.RequestEventCreation(Slug.From("e1"), Requester, Now);
        sut.RegisterEventCreated(request.Id, TicketedEventId.New(), Now);

        // Act
        sut.ExpireEventCreationRequest(request.Id, Now.AddHours(25));

        // Assert — counters unchanged from Created state
        sut.PendingEventCount.ShouldBe(0);
        sut.ActiveEventCount.ShouldBe(1);
        request.Status.ShouldBe(TeamEventCreationRequestStatus.Created);
    }

    // -------------------------------------------------------------------------
    // RegisterEventCancelled
    // -------------------------------------------------------------------------

    [TestMethod]
    public void RegisterEventCancelled_ActiveEvent_SwapsActiveToCancelled()
    {
        // Arrange
        var sut = new TeamBuilder().Build();
        var eventId = TicketedEventId.New();
        var request = sut.RequestEventCreation(Slug.From("e1"), Requester, Now);
        sut.RegisterEventCreated(request.Id, eventId, Now);

        // Act
        sut.RegisterEventCancelled(eventId);

        // Assert
        sut.ActiveEventCount.ShouldBe(0);
        sut.CancelledEventCount.ShouldBe(1);
        sut.ArchivedEventCount.ShouldBe(0);
        request.ObservedEventStatus.ShouldBe(EventStatus.Cancelled);
    }

    [TestMethod]
    public void RegisterEventCancelled_RedeliveredAfterCancel_IsIdempotent()
    {
        // Arrange
        var sut = new TeamBuilder().Build();
        var eventId = TicketedEventId.New();
        var request = sut.RequestEventCreation(Slug.From("e1"), Requester, Now);
        sut.RegisterEventCreated(request.Id, eventId, Now);
        sut.RegisterEventCancelled(eventId);

        // Act — redeliver
        sut.RegisterEventCancelled(eventId);

        // Assert — no double-decrement
        sut.ActiveEventCount.ShouldBe(0);
        sut.CancelledEventCount.ShouldBe(1);
    }

    [TestMethod]
    public void RegisterEventCancelled_UnknownEvent_IsNoOp()
    {
        // Arrange
        var sut = new TeamBuilder().Build();
        var request = sut.RequestEventCreation(Slug.From("e1"), Requester, Now);
        sut.RegisterEventCreated(request.Id, TicketedEventId.New(), Now);

        // Act
        sut.RegisterEventCancelled(TicketedEventId.New());

        // Assert
        sut.ActiveEventCount.ShouldBe(1);
        sut.CancelledEventCount.ShouldBe(0);
    }

    // -------------------------------------------------------------------------
    // RegisterEventArchived
    // -------------------------------------------------------------------------

    [TestMethod]
    public void RegisterEventArchived_FromActive_DecrementsActiveAndIncrementsArchived()
    {
        // Arrange
        var sut = new TeamBuilder().Build();
        var eventId = TicketedEventId.New();
        var request = sut.RequestEventCreation(Slug.From("e1"), Requester, Now);
        sut.RegisterEventCreated(request.Id, eventId, Now);

        // Act
        sut.RegisterEventArchived(eventId);

        // Assert
        sut.ActiveEventCount.ShouldBe(0);
        sut.CancelledEventCount.ShouldBe(0);
        sut.ArchivedEventCount.ShouldBe(1);
        request.ObservedEventStatus.ShouldBe(EventStatus.Archived);
    }

    [TestMethod]
    public void RegisterEventArchived_FromCancelled_DecrementsCancelledAndIncrementsArchived()
    {
        // Arrange
        var sut = new TeamBuilder().Build();
        var eventId = TicketedEventId.New();
        var request = sut.RequestEventCreation(Slug.From("e1"), Requester, Now);
        sut.RegisterEventCreated(request.Id, eventId, Now);
        sut.RegisterEventCancelled(eventId);

        // Act
        sut.RegisterEventArchived(eventId);

        // Assert
        sut.ActiveEventCount.ShouldBe(0);
        sut.CancelledEventCount.ShouldBe(0);
        sut.ArchivedEventCount.ShouldBe(1);
    }

    [TestMethod]
    public void RegisterEventArchived_AlreadyArchived_IsIdempotent()
    {
        // Arrange
        var sut = new TeamBuilder().Build();
        var eventId = TicketedEventId.New();
        var request = sut.RequestEventCreation(Slug.From("e1"), Requester, Now);
        sut.RegisterEventCreated(request.Id, eventId, Now);
        sut.RegisterEventArchived(eventId);

        // Act
        sut.RegisterEventArchived(eventId);

        // Assert — no double-increment
        sut.ArchivedEventCount.ShouldBe(1);
        sut.ActiveEventCount.ShouldBe(0);
    }

    // -------------------------------------------------------------------------
    // Counter invariants — no negative values
    // -------------------------------------------------------------------------

    [TestMethod]
    public void RegisterEventCancelled_FreshTeam_DoesNotDriveCountersNegative()
    {
        // Arrange — no requests at all
        var sut = new TeamBuilder().Build();

        // Act
        sut.RegisterEventCancelled(TicketedEventId.New());

        // Assert
        sut.ActiveEventCount.ShouldBe(0);
        sut.CancelledEventCount.ShouldBe(0);
    }

    [TestMethod]
    public void RegisterEventArchived_FreshTeam_DoesNotDriveCountersNegative()
    {
        // Arrange — no requests at all
        var sut = new TeamBuilder().Build();

        // Act
        sut.RegisterEventArchived(TicketedEventId.New());

        // Assert
        sut.ActiveEventCount.ShouldBe(0);
        sut.CancelledEventCount.ShouldBe(0);
        sut.ArchivedEventCount.ShouldBe(0);
    }

    // -------------------------------------------------------------------------
    // Archive guard with active/pending counts
    // -------------------------------------------------------------------------

    [TestMethod]
    public void Archive_TeamWithActiveEvent_ThrowsHasActiveOrPendingEvents()
    {
        // Arrange
        var sut = new TeamBuilder().Build();
        var request = sut.RequestEventCreation(Slug.From("e1"), Requester, Now);
        sut.RegisterEventCreated(request.Id, TicketedEventId.New(), Now);

        // Act
        var result = ErrorResult.Capture(() => sut.Archive(Now));

        // Assert
        result.Error.ShouldMatch(Team.Errors.HasActiveOrPendingEvents(sut.Id, active: 1, pending: 0));
    }

    [TestMethod]
    public void Archive_TeamWithPendingRequest_ThrowsHasActiveOrPendingEvents()
    {
        // Arrange
        var sut = new TeamBuilder().Build();
        sut.RequestEventCreation(Slug.From("e1"), Requester, Now);

        // Act
        var result = ErrorResult.Capture(() => sut.Archive(Now));

        // Assert
        result.Error.ShouldMatch(Team.Errors.HasActiveOrPendingEvents(sut.Id, active: 0, pending: 1));
    }

    [TestMethod]
    public void Archive_TeamWithOnlyCancelledOrArchivedEvents_Succeeds()
    {
        // Arrange
        var sut = new TeamBuilder().Build();
        var cancelledId = TicketedEventId.New();
        var archivedId = TicketedEventId.New();

        var r1 = sut.RequestEventCreation(Slug.From("e1"), Requester, Now);
        sut.RegisterEventCreated(r1.Id, cancelledId, Now);
        sut.RegisterEventCancelled(cancelledId);

        var r2 = sut.RequestEventCreation(Slug.From("e2"), Requester, Now);
        sut.RegisterEventCreated(r2.Id, archivedId, Now);
        sut.RegisterEventArchived(archivedId);

        // Sanity: counters as expected
        sut.ActiveEventCount.ShouldBe(0);
        sut.PendingEventCount.ShouldBe(0);
        sut.CancelledEventCount.ShouldBe(1);
        sut.ArchivedEventCount.ShouldBe(1);

        // Act
        sut.Archive(Now);

        // Assert
        sut.IsArchived.ShouldBeTrue();
    }

    [TestMethod]
    public void Archive_FreshTeam_Succeeds()
    {
        // Arrange
        var sut = new TeamBuilder().Build();

        // Act
        sut.Archive(Now);

        // Assert
        sut.IsArchived.ShouldBeTrue();
    }
}
