using System.Net;
using System.Net.Http.Json;
using Amolenk.Admitto.Api.Tests.Infrastructure;
using Shouldly;

namespace Amolenk.Admitto.Api.Tests.Registrations.GetRegistrations;

[TestClass]
public sealed class GetRegistrationsTests(TestContext testContext) : EndToEndTestBase
{
    [TestMethod]
    public async Task UnknownTeam_Returns404()
    {
        var fixture = GetRegistrationsFixture.HappyFlow();
        await fixture.SetupAsync(Environment);

        var response = await Environment.ApiClient.GetAsync(
            $"/admin/teams/{Guid.NewGuid()}/events/{Guid.NewGuid()}/registrations",
            testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [TestMethod]
    public async Task UnknownEvent_Returns404()
    {
        var fixture = GetRegistrationsFixture.HappyFlow();
        await fixture.SetupAsync(Environment);

        var response = await Environment.ApiClient.GetAsync(
            $"/admin/teams/{fixture.TeamId}/events/{Guid.NewGuid()}/registrations",
            testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [TestMethod]
    public async Task Organizer_GetsRegistrationList()
    {
        var fixture = GetRegistrationsFixture.HappyFlow();
        await fixture.SetupAsync(Environment);

        var response = await Environment.ApiClient.GetAsync(
            fixture.Route,
            testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<RegistrationItemDto[]>(
            cancellationToken: testContext.CancellationToken);
        body.ShouldNotBeNull();
        body.Length.ShouldBe(1);
        body[0].Email.ShouldBe("alice@example.com");
        body[0].Tickets.Length.ShouldBe(1);
        body[0].Tickets[0].Id.ShouldBe(GetRegistrationsFixture.TicketTypeId.Value);
    }

    [TestMethod]
    public async Task NonMember_Returns403()
    {
        var fixture = GetRegistrationsFixture.HappyFlow();
        await fixture.SetupAsync(Environment);

        var response = await Environment.BobApiClient.GetAsync(
            fixture.Route,
            testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    private sealed record RegistrationItemDto(
        Guid Id,
        string Email,
        TicketDto[] Tickets,
        DateTimeOffset CreatedAt);

    private sealed record TicketDto(Guid Id, string Name);
}
