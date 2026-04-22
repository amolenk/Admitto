using Amolenk.Admitto.Module.Registrations.Application.UseCases.Registrations.GetRegistrations;
using Amolenk.Admitto.Module.Registrations.Tests.Application.Aspire;

namespace Amolenk.Admitto.Module.Registrations.Tests.Application.UseCases.Registrations.GetRegistrations;

[TestClass]
public sealed class GetRegistrationsTests(TestContext testContext) : AspireIntegrationTestBase
{
    [TestMethod]
    public async ValueTask SC001_EmptyEvent_ReturnsEmptyList()
    {
        var fixture = GetRegistrationsFixture.Empty();
        await fixture.SetupAsync(Environment);

        var result = await NewHandler().HandleAsync(
            new GetRegistrationsQuery(fixture.EventId),
            testContext.CancellationToken);

        result.ShouldBeEmpty();
    }

    [TestMethod]
    public async ValueTask SC002_WithRegistrations_ReturnsOneItemPerRegistration()
    {
        var fixture = GetRegistrationsFixture.WithRegistrations();
        await fixture.SetupAsync(Environment);

        var result = await NewHandler().HandleAsync(
            new GetRegistrationsQuery(fixture.EventId),
            testContext.CancellationToken);

        result.Count.ShouldBe(2);

        var alice = result.SingleOrDefault(r => r.Email == "alice@example.com");
        alice.ShouldNotBeNull();
        alice.Id.ShouldNotBe(Guid.Empty);
        alice.Tickets.Count.ShouldBe(1);
        alice.Tickets[0].Slug.ShouldBe(GetRegistrationsFixture.GeneralSlug);
        alice.Tickets[0].Name.ShouldBe(GetRegistrationsFixture.GeneralName);
        alice.CreatedAt.ShouldNotBe(default);

        result.SingleOrDefault(r => r.Email == "bob@example.com").ShouldNotBeNull();
    }

    [TestMethod]
    public async ValueTask SC003_RegistrationWithMultipleTickets_SurfacesAllTickets()
    {
        var fixture = GetRegistrationsFixture.WithMultiTicketRegistration();
        await fixture.SetupAsync(Environment);

        var result = await NewHandler().HandleAsync(
            new GetRegistrationsQuery(fixture.EventId),
            testContext.CancellationToken);

        var carol = result.ShouldHaveSingleItem();
        carol.Email.ShouldBe("carol@example.com");
        carol.Tickets.Count.ShouldBe(2);

        var general = carol.Tickets.SingleOrDefault(t => t.Slug == GetRegistrationsFixture.GeneralSlug);
        general.ShouldNotBeNull().Name.ShouldBe(GetRegistrationsFixture.GeneralName);

        var vip = carol.Tickets.SingleOrDefault(t => t.Slug == GetRegistrationsFixture.VipSlug);
        vip.ShouldNotBeNull().Name.ShouldBe(GetRegistrationsFixture.VipName);
    }

    [TestMethod]
    public async ValueTask SC004_OnlyReturnsRegistrationsForRequestedEvent()
    {
        var fixture = GetRegistrationsFixture.WithRegistrationsAcrossEvents();
        await fixture.SetupAsync(Environment);

        var result = await NewHandler().HandleAsync(
            new GetRegistrationsQuery(fixture.EventId),
            testContext.CancellationToken);

        result.Count.ShouldBe(2);
        result.ShouldNotContain(r => r.Email == "dave@example.com");

        var otherResult = await NewHandler().HandleAsync(
            new GetRegistrationsQuery(fixture.OtherEventId),
            testContext.CancellationToken);

        otherResult.ShouldHaveSingleItem().Email.ShouldBe("dave@example.com");
    }

    private static GetRegistrationsHandler NewHandler() =>
        new(Environment.Database.Context);
}
