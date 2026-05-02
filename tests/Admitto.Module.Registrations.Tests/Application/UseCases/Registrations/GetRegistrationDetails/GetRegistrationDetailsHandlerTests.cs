using Amolenk.Admitto.Module.Registrations.Application.UseCases.Registrations.GetRegistrationDetails;
using Amolenk.Admitto.Module.Registrations.Contracts;
using Amolenk.Admitto.Module.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Module.Registrations.Tests.Application.Aspire;

namespace Amolenk.Admitto.Module.Registrations.Tests.Application.UseCases.Registrations.GetRegistrationDetails;

[TestClass]
public sealed class GetRegistrationDetailsHandlerTests(TestContext testContext) : AspireIntegrationTestBase
{
    [TestMethod]
    public async ValueTask SC001_ActiveRegistration_ReturnsFullDetail()
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
        result.Tickets.ShouldHaveSingleItem().Slug.ShouldBe(GetRegistrationDetailsFixture.TicketTypeSlug);
        result.Tickets[0].Name.ShouldBe(GetRegistrationDetailsFixture.TicketTypeName);
        result.Activities.ShouldHaveSingleItem().ActivityType.ShouldBe(nameof(ActivityType.Registered));
    }

    [TestMethod]
    public async ValueTask SC002_ReconfirmedRegistration_ReturnsReconfirmedStatus()
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

    [TestMethod]
    public async ValueTask SC003_CancelledRegistration_ReturnsCancelledStatusAndReason()
    {
        var fixture = GetRegistrationDetailsFixture.WithCancelledAttendee();
        await fixture.SetupAsync(Environment);

        var result = await NewHandler().HandleAsync(
            new GetRegistrationDetailsQuery(fixture.TeamId.Value, fixture.EventId, fixture.RegistrationId),
            testContext.CancellationToken);

        result.ShouldNotBeNull();
        result.Status.ShouldBe(RegistrationStatus.Cancelled);
        result.CancellationReason.ShouldBe("AttendeeRequest");
        result.Activities.Count.ShouldBe(2);
        result.Activities.ShouldContain(a => a.ActivityType == nameof(ActivityType.Cancelled));
    }

    [TestMethod]
    public async ValueTask SC004_RegistrationWithAdditionalDetails_ReturnsDictionary()
    {
        var fixture = GetRegistrationDetailsFixture.WithAdditionalDetails();
        await fixture.SetupAsync(Environment);

        var result = await NewHandler().HandleAsync(
            new GetRegistrationDetailsQuery(fixture.TeamId.Value, fixture.EventId, fixture.RegistrationId),
            testContext.CancellationToken);

        result.ShouldNotBeNull();
        result.AdditionalDetails.ShouldContainKeyAndValue("dietary", "vegan");
    }

    [TestMethod]
    public async ValueTask SC005_MultipleTickets_ReturnsAllTickets()
    {
        var fixture = GetRegistrationDetailsFixture.WithMultipleTickets();
        await fixture.SetupAsync(Environment);

        var result = await NewHandler().HandleAsync(
            new GetRegistrationDetailsQuery(fixture.TeamId.Value, fixture.EventId, fixture.RegistrationId),
            testContext.CancellationToken);

        result.ShouldNotBeNull();
        result.Tickets.Count.ShouldBe(2);
        result.Tickets.ShouldContain(t => t.Slug == GetRegistrationDetailsFixture.TicketTypeSlug);
        result.Tickets.ShouldContain(t => t.Slug == GetRegistrationDetailsFixture.VipSlug);
    }

    [TestMethod]
    public async ValueTask SC006_UnknownRegistrationId_ReturnsNull()
    {
        var fixture = GetRegistrationDetailsFixture.WithRegisteredAttendee();
        await fixture.SetupAsync(Environment);

        var result = await NewHandler().HandleAsync(
            new GetRegistrationDetailsQuery(fixture.TeamId.Value, fixture.EventId, RegistrationId.New()),
            testContext.CancellationToken);

        result.ShouldBeNull();
    }

    [TestMethod]
    public async ValueTask SC007_RegistrationExistsButDifferentEvent_ReturnsNull()
    {
        var fixture = GetRegistrationDetailsFixture.WithRegisteredAttendee();
        await fixture.SetupAsync(Environment);

        var result = await NewHandler().HandleAsync(
            new GetRegistrationDetailsQuery(fixture.TeamId.Value, fixture.OtherEventId, fixture.RegistrationId),
            testContext.CancellationToken);

        result.ShouldBeNull();
    }

    [TestMethod]
    public async ValueTask SC008_ActivitiesReturnedInChronologicalOrder()
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
        new(Environment.Database.Context);
}
