using Amolenk.Admitto.Core.Registrations.Application.UseCases.Registrations.GetRegistrations;

namespace Amolenk.Admitto.Core.IntegrationTests.Registrations.Application.UseCases.Registrations.GetRegistrations;

[TestClass]
public sealed class GetRegistrationsTests(TestContext testContext) : AspireIntegrationTestBase
{
    // Given an event with no registrations
    // When the registrations are queried
    // Then an empty list is returned
    [TestMethod]
    public async ValueTask EmptyEvent_ReturnsEmptyList()
    {
        var fixture = GetRegistrationsFixture.Empty();
        await fixture.SetupAsync(Environment);

        var result = await NewHandler().HandleAsync(
            new GetRegistrationsQuery(fixture.EventId, fixture.TeamId),
            testContext.CancellationToken);

        result.ShouldNotBeNull().ShouldBeEmpty();
    }

    // Given an event with two registrations
    // When the registrations are queried
    // Then one result item is returned per registration with its ticket details
    [TestMethod]
    public async ValueTask WithRegistrations_ReturnsOneItemPerRegistration()
    {
        var fixture = GetRegistrationsFixture.WithRegistrations();
        await fixture.SetupAsync(Environment);

        var result = await NewHandler().HandleAsync(
            new GetRegistrationsQuery(fixture.EventId, fixture.TeamId),
            testContext.CancellationToken);

        result.ShouldNotBeNull();
        result.Count.ShouldBe(2);

        var alice = result.SingleOrDefault(r => r.Email == "alice@example.com");
        alice.ShouldNotBeNull();
        alice.Id.ShouldNotBe(Guid.Empty);
        alice.Tickets.Count.ShouldBe(1);
        alice.Tickets[0].Id.ShouldBe(fixture.GeneralId.Value);
        alice.Tickets[0].Name.ShouldBe(GetRegistrationsFixture.GeneralName);
        alice.CreatedAt.ShouldNotBe(default);

        result.SingleOrDefault(r => r.Email == "bob@example.com").ShouldNotBeNull();
    }

    // Given a registration that holds multiple tickets
    // When the registrations are queried
    // Then all of that registration's tickets are surfaced
    [TestMethod]
    public async ValueTask RegistrationWithMultipleTickets_SurfacesAllTickets()
    {
        var fixture = GetRegistrationsFixture.WithMultiTicketRegistration();
        await fixture.SetupAsync(Environment);

        var result = await NewHandler().HandleAsync(
            new GetRegistrationsQuery(fixture.EventId, fixture.TeamId),
            testContext.CancellationToken);

        result.ShouldNotBeNull();
        var carol = result.ShouldHaveSingleItem();
        carol.Email.ShouldBe("carol@example.com");
        carol.Tickets.Count.ShouldBe(2);

        var general = carol.Tickets.SingleOrDefault(t => t.Id == fixture.GeneralId.Value);
        general.ShouldNotBeNull().Name.ShouldBe(GetRegistrationsFixture.GeneralName);

        var vip = carol.Tickets.SingleOrDefault(t => t.Id == fixture.VipId.Value);
        vip.ShouldNotBeNull().Name.ShouldBe(GetRegistrationsFixture.VipName);
    }

    // Given registrations spread across two different events
    // When the registrations are queried for one specific event
    // Then only that event's registrations are returned, excluding the other event's
    [TestMethod]
    public async ValueTask OnlyReturnsRegistrationsForRequestedEvent()
    {
        var fixture = GetRegistrationsFixture.WithRegistrationsAcrossEvents();
        await fixture.SetupAsync(Environment);

        var result = await NewHandler().HandleAsync(
            new GetRegistrationsQuery(fixture.EventId, fixture.TeamId),
            testContext.CancellationToken);

        result.ShouldNotBeNull();
        result.Count.ShouldBe(2);
        result.ShouldNotContain(r => r.Email == "dave@example.com");

        var otherResult = await NewHandler().HandleAsync(
            new GetRegistrationsQuery(fixture.OtherEventId, fixture.TeamId),
            testContext.CancellationToken);

        otherResult.ShouldNotBeNull().ShouldHaveSingleItem().Email.ShouldBe("dave@example.com");
    }

    private static GetRegistrationsHandler NewHandler() =>
        new(Environment.RegistrationsDatabase.Context);
}
