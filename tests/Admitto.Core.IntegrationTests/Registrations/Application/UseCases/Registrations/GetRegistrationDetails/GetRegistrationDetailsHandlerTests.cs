using Amolenk.Admitto.Core.Registrations.Application.UseCases.Registrations.GetRegistrationDetails;
using Amolenk.Admitto.Core.Registrations.Contracts;
using Amolenk.Admitto.Core.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Core.Registrations.Contracts.ValueObjects;

namespace Amolenk.Admitto.Core.IntegrationTests.Registrations.Application.UseCases.Registrations.GetRegistrationDetails;

[TestClass]
public sealed class GetRegistrationDetailsHandlerTests(TestContext testContext) : AspireIntegrationTestBase
{
    // Given an active registration with a registered attendee
    // When the registration details are queried
    // Then the full detail including tickets and activities is returned
    [TestMethod]
    public async ValueTask ActiveRegistration_ReturnsFullDetail()
    {
        var fixture = GetRegistrationDetailsFixture.WithRegisteredAttendee();
        await fixture.SetupAsync(Environment);

        var result = await NewHandler().HandleAsync(
            new GetRegistrationDetailsQuery(fixture.TeamId.Value, fixture.EventId, fixture.RegistrationId),
            testContext.CancellationToken);

        result.ShouldNotBeNull();
        result.Id.ShouldBe(fixture.RegistrationId.Value);
        result.Email.ShouldBe("alice@example.com");
        result.FirstName.ShouldBe("Alice");
        result.LastName.ShouldBe("Doe");
        result.Status.ShouldBe(RegistrationStatus.Registered);
        result.HasReconfirmed.ShouldBeFalse();
        result.ReconfirmedAt.ShouldBeNull();
        result.CancellationReason.ShouldBeNull();
        result.Tickets.ShouldHaveSingleItem().Id.ShouldBe(fixture.TicketTypeId.Value);
        result.Tickets[0].Name.ShouldBe(GetRegistrationDetailsFixture.TicketTypeNameStr);
        result.Activities.ShouldHaveSingleItem().ActivityType.ShouldBe(nameof(ActivityType.Registered));
    }

    // Given a registration whose attendee has reconfirmed
    // When the registration details are queried
    // Then it reports the reconfirmed timestamp and both registered and reconfirmed activities
    [TestMethod]
    public async ValueTask ReconfirmedRegistration_ReturnsReconfirmedStatus()
    {
        var fixture = GetRegistrationDetailsFixture.WithReconfirmedAttendee();
        await fixture.SetupAsync(Environment);

        var result = await NewHandler().HandleAsync(
            new GetRegistrationDetailsQuery(fixture.TeamId.Value, fixture.EventId, fixture.RegistrationId),
            testContext.CancellationToken);

        result.ShouldNotBeNull();
        result.HasReconfirmed.ShouldBeTrue();
        result.ReconfirmedAt.ShouldNotBeNull();
        result.Status.ShouldBe(RegistrationStatus.Registered);
        result.Activities.Count.ShouldBe(2);
        result.Activities.ShouldContain(a => a.ActivityType == nameof(ActivityType.Registered));
        result.Activities.ShouldContain(a => a.ActivityType == nameof(ActivityType.Reconfirmed));
    }

    // Given a cancelled registration
    // When the registration details are queried
    // Then the cancelled status, cancellation reason, and cancellation activity are returned
    [TestMethod]
    public async ValueTask CancelledRegistration_ReturnsCancelledStatus()
    {
        var fixture = GetRegistrationDetailsFixture.WithCancelledAttendee();
        await fixture.SetupAsync(Environment);

        var result = await NewHandler().HandleAsync(
            new GetRegistrationDetailsQuery(fixture.TeamId.Value, fixture.EventId, fixture.RegistrationId),
            testContext.CancellationToken);

        result.ShouldNotBeNull();
        result.Status.ShouldBe(RegistrationStatus.Cancelled);
        result.CancellationReason.ShouldNotBeNull();
        result.Activities.Count.ShouldBe(2);
        result.Activities.ShouldContain(a => a.ActivityType == nameof(ActivityType.Cancelled));
    }

    // Given a registration with additional details captured
    // When the registration details are queried
    // Then the additional details are included in the result
    [TestMethod]
    public async ValueTask RegistrationWithAdditionalDetails_ReturnsDetails()
    {
        var fixture = GetRegistrationDetailsFixture.WithAdditionalDetails();
        await fixture.SetupAsync(Environment);

        var result = await NewHandler().HandleAsync(
            new GetRegistrationDetailsQuery(fixture.TeamId.Value, fixture.EventId, fixture.RegistrationId),
            testContext.CancellationToken);

        result.ShouldNotBeNull();
        result.AdditionalDetails.ShouldNotBeNull();
        result.AdditionalDetails["dietary"].ShouldBe("vegan");
    }

    // Given a registration with multiple tickets
    // When the registration details are queried
    // Then both tickets are returned
    [TestMethod]
    public async ValueTask RegistrationWithMultipleTickets_ReturnsBothTickets()
    {
        var fixture = GetRegistrationDetailsFixture.WithMultipleTickets();
        await fixture.SetupAsync(Environment);

        var result = await NewHandler().HandleAsync(
            new GetRegistrationDetailsQuery(fixture.TeamId.Value, fixture.EventId, fixture.RegistrationId),
            testContext.CancellationToken);

        result.ShouldNotBeNull();
        result.Tickets.Count.ShouldBe(2);
        result.Tickets.ShouldContain(t => t.Id == fixture.TicketTypeId.Value);
        result.Tickets.ShouldContain(t => t.Id == fixture.VipId.Value);
    }

    // Given no registration exists for the given registration id
    // When the registration details are queried
    // Then null is returned
    [TestMethod]
    public async ValueTask UnknownRegistrationId_ReturnsNull()
    {
        var fixture = GetRegistrationDetailsFixture.WithRegisteredAttendee();
        await fixture.SetupAsync(Environment);

        var result = await NewHandler().HandleAsync(
            new GetRegistrationDetailsQuery(fixture.TeamId.Value, fixture.EventId, RegistrationId.New()),
            testContext.CancellationToken);

        result.ShouldBeNull();
    }

    // Given a registration that exists but belongs to a different event
    // When the registration details are queried using another event's id
    // Then null is returned
    [TestMethod]
    public async ValueTask RegistrationExistsButDifferentEvent_ReturnsNull()
    {
        var fixture = GetRegistrationDetailsFixture.WithRegisteredAttendee();
        await fixture.SetupAsync(Environment);

        var result = await NewHandler().HandleAsync(
            new GetRegistrationDetailsQuery(fixture.TeamId.Value, fixture.OtherEventId, fixture.RegistrationId),
            testContext.CancellationToken);

        result.ShouldBeNull();
    }

    // Given a registration with a reconfirmed attendee
    // When the registration details are queried
    // Then the activities are returned in chronological order
    [TestMethod]
    public async ValueTask ActivitiesReturnedInChronologicalOrder()
    {
        var fixture = GetRegistrationDetailsFixture.WithReconfirmedAttendee();
        await fixture.SetupAsync(Environment);

        var result = await NewHandler().HandleAsync(
            new GetRegistrationDetailsQuery(fixture.TeamId.Value, fixture.EventId, fixture.RegistrationId),
            testContext.CancellationToken);

        result.ShouldNotBeNull();
        result.Activities.Count.ShouldBe(2);
        result.Activities[0].ActivityType.ShouldBe(nameof(ActivityType.Registered));
        result.Activities[1].ActivityType.ShouldBe(nameof(ActivityType.Reconfirmed));
        result.Activities[0].OccurredAt.ShouldBeLessThan(result.Activities[1].OccurredAt);
    }

    private static GetRegistrationDetailsHandler NewHandler() =>
        new(Environment.RegistrationsDatabase.Context, Environment.RegistrationsDatabase.Context);
}
