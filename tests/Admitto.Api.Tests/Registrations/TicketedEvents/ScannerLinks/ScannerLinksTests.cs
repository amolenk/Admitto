using System.Net;
using System.Net.Http.Json;
using Amolenk.Admitto.Api.Tests.Infrastructure;
using Amolenk.Admitto.Core.Registrations.Application.UseCases.TicketedEvents.ScannerLinks;
using Shouldly;

namespace Amolenk.Admitto.Api.Tests.Registrations.TicketedEvents.ScannerLinks;

[TestClass]
public sealed class ScannerLinksTests(TestContext testContext) : EndToEndTestBase
{
    // Given an active event
    // When an organizer creates, views, regenerates, and revokes its scanner link
    // Then each operation succeeds and retired links expose no URL
    [TestMethod]
    public async Task ScannerLink_Organizer_ManagesFullLifecycle()
    {
        var fixture = ScannerLinksFixture.Organizer();
        await fixture.SetupAsync(Environment);

        var create = await Environment.ApiClient.PostAsync(fixture.Route, null, testContext.CancellationToken);
        create.StatusCode.ShouldBe(HttpStatusCode.OK);
        var created = await create.Content.ReadFromJsonAsync<ScannerLinkDto>(testContext.CancellationToken);
        created.ShouldNotBeNull();
        created.Status.ShouldBe("Active");
        created.Url.ShouldNotBeNull();

        var get = await Environment.ApiClient.GetAsync(fixture.Route, testContext.CancellationToken);
        var viewed = await get.Content.ReadFromJsonAsync<ScannerLinkDto>(testContext.CancellationToken);
        get.StatusCode.ShouldBe(HttpStatusCode.OK);
        viewed.ShouldNotBeNull();
        viewed.Status.ShouldBe("Active");
        viewed.Url.ShouldBe(created.Url);

        var regenerate = await Environment.ApiClient.PostAsync(
            fixture.RegenerateRoute, null, testContext.CancellationToken);
        regenerate.StatusCode.ShouldBe(HttpStatusCode.OK);
        var regenerated = await regenerate.Content.ReadFromJsonAsync<ScannerLinkDto>(testContext.CancellationToken);
        regenerated.ShouldNotBeNull();
        regenerated.Url.ShouldNotBe(created.Url);

        var revoke = await Environment.ApiClient.PostAsync(
            fixture.RevokeRoute, null, testContext.CancellationToken);
        revoke.StatusCode.ShouldBe(HttpStatusCode.OK);
        var revoked = await revoke.Content.ReadFromJsonAsync<ScannerLinkDto>(testContext.CancellationToken);
        revoked.ShouldNotBeNull();
        revoked.Status.ShouldBe("Revoked");
        revoked.Url.ShouldBeNull();
    }

    // Given an active event
    // When a Crew member calls scanner-link management endpoints
    // Then every endpoint returns Forbidden
    [TestMethod]
    public async Task ScannerLink_CrewMember_AllOperationsReturn403()
    {
        var fixture = ScannerLinksFixture.CrewMember();
        await fixture.SetupAsync(Environment);

        foreach (var request in new[]
                 {
                     () => Environment.BobApiClient.GetAsync(fixture.Route, testContext.CancellationToken),
                     () => Environment.BobApiClient.PostAsync(fixture.Route, null, testContext.CancellationToken),
                     () => Environment.BobApiClient.PostAsync(fixture.RegenerateRoute, null, testContext.CancellationToken),
                     () => Environment.BobApiClient.PostAsync(fixture.RevokeRoute, null, testContext.CancellationToken)
                 })
        {
            var response = await request();
            response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        }
    }

    // Given an active event
    // When an unauthenticated caller uses scanner-link management endpoints
    // Then every endpoint returns Unauthorized
    [TestMethod]
    public async Task ScannerLink_Unauthenticated_AllOperationsReturn401()
    {
        var fixture = ScannerLinksFixture.Organizer();
        await fixture.SetupAsync(Environment);

        foreach (var request in new[]
                 {
                     () => Environment.AnonymousApiClient.GetAsync(fixture.Route, testContext.CancellationToken),
                     () => Environment.AnonymousApiClient.PostAsync(fixture.Route, null, testContext.CancellationToken),
                     () => Environment.AnonymousApiClient.PostAsync(fixture.RegenerateRoute, null, testContext.CancellationToken),
                     () => Environment.AnonymousApiClient.PostAsync(fixture.RevokeRoute, null, testContext.CancellationToken)
                 })
        {
            var response = await request();
            response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        }
    }
}
