using System.Net;
using System.Net.Http.Json;
using Amolenk.Admitto.Api.Tests.Infrastructure;
using Shouldly;

namespace Amolenk.Admitto.Api.Tests.Registrations.GetRegistrations;

[TestClass]
public sealed class GetRegistrationsTests(TestContext testContext) : EndToEndTestBase
{
    [TestMethod]
    public async Task SC005_UnknownTeam_Returns404()
    {
        var fixture = GetRegistrationsFixture.HappyFlow();
        await fixture.SetupAsync(Environment);

        var response = await Environment.ApiClient.GetAsync(
            GetRegistrationsFixture.Route(teamSlug: "ghost-team"),
            testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [TestMethod]
    public async Task SC006_UnknownEvent_Returns404()
    {
        var fixture = GetRegistrationsFixture.HappyFlow();
        await fixture.SetupAsync(Environment);

        var response = await Environment.ApiClient.GetAsync(
            GetRegistrationsFixture.Route(eventSlug: "ghost-event"),
            testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [TestMethod]
    public async Task SC007_Organizer_GetsRegistrationList()
    {
        var fixture = GetRegistrationsFixture.HappyFlow();
        await fixture.SetupAsync(Environment);

        var response = await Environment.ApiClient.GetAsync(
            GetRegistrationsFixture.Route(),
            testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<RegistrationItemDto[]>(
            cancellationToken: testContext.CancellationToken);
        body.ShouldNotBeNull();
        body.Length.ShouldBe(1);
        body[0].Email.ShouldBe("alice@example.com");
        body[0].Tickets.Length.ShouldBe(1);
        body[0].Tickets[0].Slug.ShouldBe(GetRegistrationsFixture.TicketTypeSlug);
    }

    [TestMethod]
    public async Task SC008_NonMember_Returns403()
    {
        var fixture = GetRegistrationsFixture.HappyFlow();
        await fixture.SetupAsync(Environment);

        var response = await Environment.BobApiClient.GetAsync(
            GetRegistrationsFixture.Route(),
            testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    private sealed record RegistrationItemDto(
        Guid Id,
        string Email,
        TicketDto[] Tickets,
        DateTimeOffset CreatedAt);

    private sealed record TicketDto(string Slug, string Name);
}
