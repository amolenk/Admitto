using Amolenk.Admitto.Core.Organization.Domain.Entities;
using Amolenk.Admitto.Testing.Builders.Organization.Domain;
using Amolenk.Admitto.Core.Organization.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;
using Amolenk.Admitto.Testing.Infrastructure.Assertions;
using Shouldly;

namespace Amolenk.Admitto.Core.Organization.Domain.Tests.Entities;

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

    // Given an active team
    // When an event creation is requested
    // Then a pending request is added and the pending event counter is incremented
    [TestMethod]
    public void RequestEventCreation_ActiveTeam_AddsPendingRequestAndIncrementsPendingCounter()
    {
        // Arrange
        var sut = new TeamBuilder().Build();

        // Act
        var request = sut.RequestEventCreation(Requester, Now);

        // Assert
        sut.PendingEventCount.ShouldBe(1);
        sut.ActiveEventCount.ShouldBe(0);
        sut.EventCreationRequests.ShouldContain(request);
        request.RequesterId.ShouldBe(Requester);
        request.RequestedAt.ShouldBe(Now);
        request.Status.ShouldBe(TeamEventCreationRequestStatus.Pending);
    }

    // Given an archived team
    // When an event creation is requested
    // Then it throws TeamArchived
    [TestMethod]
    public void RequestEventCreation_ArchivedTeam_ThrowsTeamArchived()
    {
        // Arrange
        var sut = new TeamBuilder().AsArchived().Build();

        // Act
        var result = ErrorResult.Capture(() =>
            sut.RequestEventCreation(Requester, Now));

        // Assert
        result.Error.ShouldMatch(Team.Errors.TeamArchived(sut.Id));
    }

    // -------------------------------------------------------------------------
    // RegisterEventCreated
    // -------------------------------------------------------------------------

    // Given a team with a pending event creation request
    // When the event creation is registered as complete
    // Then the request transitions to Created and the pending counter moves to the active counter
    [TestMethod]
    public void RegisterEventCreated_PendingRequest_TransitionsAndSwapsCounters()
    {
        // Arrange
        var sut = new TeamBuilder().Build();
        var request = sut.RequestEventCreation(Requester, Now);
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

    // Given a request whose event creation was already registered
    // When the same event creation is registered again
    // Then the counters remain unchanged
    [TestMethod]
    public void RegisterEventCreated_AlreadyTerminal_IsIdempotent()
    {
        // Arrange
        var sut = new TeamBuilder().Build();
        var request = sut.RequestEventCreation(Requester, Now);
        var eventId = TicketedEventId.New();
        sut.RegisterEventCreated(request.Id, eventId, Now);

        // Act — redeliver the same event
        sut.RegisterEventCreated(request.Id, eventId, Now);

        // Assert — counters unchanged
        sut.PendingEventCount.ShouldBe(0);
        sut.ActiveEventCount.ShouldBe(1);
    }

    // Given a team with a pending event creation request
    // When an event creation is registered for an unrelated, unknown request id
    // Then the counters are unaffected
    [TestMethod]
    public void RegisterEventCreated_UnknownRequestId_IsNoOp()
    {
        // Arrange
        var sut = new TeamBuilder().Build();
        sut.RequestEventCreation(Requester, Now);

        // Act
        sut.RegisterEventCreated(CreationRequestId.New(), TicketedEventId.New(), Now);

        // Assert
        sut.PendingEventCount.ShouldBe(1);
        sut.ActiveEventCount.ShouldBe(0);
    }

    // -------------------------------------------------------------------------
    // RegisterEventCreationRejected
    // -------------------------------------------------------------------------

    // Given a team with a pending event creation request
    // When the event creation is registered as rejected with a reason
    // Then the request transitions to Rejected with that reason and the pending counter is decremented
    [TestMethod]
    public void RegisterEventCreationRejected_PendingRequest_TransitionsAndDecrementsPending()
    {
        // Arrange
        var sut = new TeamBuilder().Build();
        var request = sut.RequestEventCreation(Requester, Now);

        // Act
        sut.RegisterEventCreationRejected(request.Id, "duplicate_slug", Now.AddSeconds(2));

        // Assert
        sut.PendingEventCount.ShouldBe(0);
        sut.ActiveEventCount.ShouldBe(0);
        request.Status.ShouldBe(TeamEventCreationRequestStatus.Rejected);
        request.RejectionReason.ShouldBe("duplicate_slug");
    }

    // Given a request whose event creation was already registered as rejected
    // When the rejection is registered again
    // Then the pending counter is not decremented further
    [TestMethod]
    public void RegisterEventCreationRejected_AlreadyTerminal_IsIdempotent()
    {
        // Arrange
        var sut = new TeamBuilder().Build();
        var request = sut.RequestEventCreation(Requester, Now);
        sut.RegisterEventCreationRejected(request.Id, "duplicate_slug", Now);

        // Act
        sut.RegisterEventCreationRejected(request.Id, "duplicate_slug", Now);

        // Assert
        sut.PendingEventCount.ShouldBe(0);
    }

    // -------------------------------------------------------------------------
    // ExpireEventCreationRequest
    // -------------------------------------------------------------------------

    // Given a team with a pending event creation request
    // When the request is expired
    // Then it transitions to Expired and the pending counter is decremented
    [TestMethod]
    public void ExpireEventCreationRequest_PendingRequest_TransitionsAndDecrementsPending()
    {
        // Arrange
        var sut = new TeamBuilder().Build();
        var request = sut.RequestEventCreation(Requester, Now);

        // Act
        sut.ExpireEventCreationRequest(request.Id, Now.AddHours(25));

        // Assert
        sut.PendingEventCount.ShouldBe(0);
        request.Status.ShouldBe(TeamEventCreationRequestStatus.Expired);
    }

    // Given a request whose event creation was already registered as Created
    // When the request is expired
    // Then the request stays Created and the counters are unchanged
    [TestMethod]
    public void ExpireEventCreationRequest_AlreadyTerminal_IsIdempotent()
    {
        // Arrange
        var sut = new TeamBuilder().Build();
        var request = sut.RequestEventCreation(Requester, Now);
        sut.RegisterEventCreated(request.Id, TicketedEventId.New(), Now);

        // Act
        sut.ExpireEventCreationRequest(request.Id, Now.AddHours(25));

        // Assert — counters unchanged from Created state
        sut.PendingEventCount.ShouldBe(0);
        sut.ActiveEventCount.ShouldBe(1);
        request.Status.ShouldBe(TeamEventCreationRequestStatus.Created);
    }

    // -------------------------------------------------------------------------
    // RegisterEventArchived
    // -------------------------------------------------------------------------

    // Given a team with an active event
    // When the event is registered as archived
    // Then the active counter is decremented and the archived counter is incremented
    [TestMethod]
    public void RegisterEventArchived_FromActive_DecrementsActiveAndIncrementsArchived()
    {
        // Arrange
        var sut = new TeamBuilder().Build();
        var eventId = TicketedEventId.New();
        var request = sut.RequestEventCreation(Requester, Now);
        sut.RegisterEventCreated(request.Id, eventId, Now);

        // Act
        sut.RegisterEventArchived(eventId);

        // Assert
        sut.ActiveEventCount.ShouldBe(0);
        sut.ArchivedEventCount.ShouldBe(1);
        request.ObservedEventStatus.ShouldBe(EventStatus.Archived);
    }

    // Given an event that has already been registered as archived
    // When the event is registered as archived again
    // Then the archived counter is not incremented a second time
    [TestMethod]
    public void RegisterEventArchived_AlreadyArchived_IsIdempotent()
    {
        // Arrange
        var sut = new TeamBuilder().Build();
        var eventId = TicketedEventId.New();
        var request = sut.RequestEventCreation(Requester, Now);
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

    // Given a fresh team with no event requests at all
    // When an unrelated event id is registered as archived
    // Then the active and archived counters stay at zero
    [TestMethod]
    public void RegisterEventArchived_FreshTeam_DoesNotDriveCountersNegative()
    {
        // Arrange — no requests at all
        var sut = new TeamBuilder().Build();

        // Act
        sut.RegisterEventArchived(TicketedEventId.New());

        // Assert
        sut.ActiveEventCount.ShouldBe(0);
        sut.ArchivedEventCount.ShouldBe(0);
    }

    // -------------------------------------------------------------------------
    // Archive guard with active/pending counts
    // -------------------------------------------------------------------------

    // Given a team with one active event
    // When the team is archived
    // Then it throws HasActiveOrPendingEvents reporting one active and zero pending events
    [TestMethod]
    public void Archive_TeamWithActiveEvent_ThrowsHasActiveOrPendingEvents()
    {
        // Arrange
        var sut = new TeamBuilder().Build();
        var request = sut.RequestEventCreation(Requester, Now);
        sut.RegisterEventCreated(request.Id, TicketedEventId.New(), Now);

        // Act
        var result = ErrorResult.Capture(() => sut.Archive(Now));

        // Assert
        result.Error.ShouldMatch(Team.Errors.HasActiveOrPendingEvents(sut.Id, active: 1, pending: 0));
    }

    // Given a team with one pending event creation request
    // When the team is archived
    // Then it throws HasActiveOrPendingEvents reporting zero active and one pending event
    [TestMethod]
    public void Archive_TeamWithPendingRequest_ThrowsHasActiveOrPendingEvents()
    {
        // Arrange
        var sut = new TeamBuilder().Build();
        sut.RequestEventCreation(Requester, Now);

        // Act
        var result = ErrorResult.Capture(() => sut.Archive(Now));

        // Assert
        result.Error.ShouldMatch(Team.Errors.HasActiveOrPendingEvents(sut.Id, active: 0, pending: 1));
    }

    // Given a team whose only event has already been archived
    // When the team is archived
    // Then the team is successfully archived
    [TestMethod]
    public void Archive_TeamWithOnlyCancelledOrArchivedEvents_Succeeds()
    {
        // Arrange
        var sut = new TeamBuilder().Build();
        var archivedId = TicketedEventId.New();

        var r2 = sut.RequestEventCreation(Requester, Now);
        sut.RegisterEventCreated(r2.Id, archivedId, Now);
        sut.RegisterEventArchived(archivedId);

        // Sanity: counters as expected
        sut.ActiveEventCount.ShouldBe(0);
        sut.PendingEventCount.ShouldBe(0);
        sut.ArchivedEventCount.ShouldBe(1);

        // Act
        sut.Archive(Now);

        // Assert
        sut.IsArchived.ShouldBeTrue();
    }

    // Given a fresh team with no events at all
    // When the team is archived
    // Then the team is successfully archived
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
