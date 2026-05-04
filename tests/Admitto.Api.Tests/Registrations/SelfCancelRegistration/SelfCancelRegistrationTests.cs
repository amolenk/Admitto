using System.Net;
using Amolenk.Admitto.Api.Tests.Infrastructure;
using Shouldly;

namespace Amolenk.Admitto.Api.Tests.Registrations.SelfCancelRegistration;

[TestClass]
public sealed class SelfCancelRegistrationTests(TestContext testContext) : EndToEndTestBase
{
    // SC001: Successful self-service cancellation returns 200
    [TestMethod]
    public async Task SC001_SelfCancelRegistration_WithoutToken_Returns200()
    {
        var fixture = SelfCancelRegistrationFixture.WithActiveRegistration();
        await fixture.SetupAsync(Environment);

        using var client = Environment.CreatePublicApiClient(fixture.ApiKey);
        var response = await client.PostAsync(fixture.CancelRoute, null, testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    // SC002: Registration not found returns 404
    [TestMethod]
    public async Task SC002_SelfCancelRegistration_NotFound_Returns404()
    {
        var fixture = SelfCancelRegistrationFixture.WithActiveRegistration();
        await fixture.SetupAsync(Environment);

        var unknownRoute = $"/api/teams/{SelfCancelRegistrationFixture.TeamSlug}/events/{SelfCancelRegistrationFixture.EventSlug}/registrations/{Guid.NewGuid()}/cancel";

        using var client = Environment.CreatePublicApiClient(fixture.ApiKey);
        var response = await client.PostAsync(unknownRoute, null, testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    // SC003: Already cancelled registration returns 409 Conflict
    [TestMethod]
    public async Task SC003_SelfCancelRegistration_AlreadyCancelled_Returns409()
    {
        var fixture = SelfCancelRegistrationFixture.WithCancelledRegistration();
        await fixture.SetupAsync(Environment, alreadyCancelled: true);

        using var client = Environment.CreatePublicApiClient(fixture.ApiKey);
        var response = await client.PostAsync(fixture.CancelRoute, null, testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }
}
