using System.Net;
using Amolenk.Admitto.Api.Tests.Infrastructure;
using Shouldly;

namespace Amolenk.Admitto.Api.Tests.Registrations.SelfCancelRegistration;

[TestClass]
public sealed class SelfCancelRegistrationTests(TestContext testContext) : EndToEndTestBase
{
    // Successful self-service cancellation returns 204 NoContent
    // Given an active registration and a valid partner API key
    // When the attendee self-cancels the registration
    // Then the API returns 204 No Content
    [TestMethod]
    public async Task SelfCancelRegistration_WithoutToken_Returns204()
    {
        var fixture = SelfCancelRegistrationFixture.WithActiveRegistration();
        await fixture.SetupAsync(Environment);

        using var client = Environment.CreatePartnerApiClient(fixture.ApiKey);
        var response = await client.PostAsync(fixture.CancelRoute, null, testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }

    // Registration not found returns 404
    // Given an active registration for an event, and a valid partner API key
    // When an unknown registration id is self-cancelled
    // Then the API returns 404 Not Found
    [TestMethod]
    public async Task SelfCancelRegistration_NotFound_Returns404()
    {
        var fixture = SelfCancelRegistrationFixture.WithActiveRegistration();
        await fixture.SetupAsync(Environment);

        var unknownRoute = $"/api/events/{fixture.EventSlug}/registrations/{Guid.NewGuid()}/cancel";

        using var client = Environment.CreatePartnerApiClient(fixture.ApiKey);
        var response = await client.PostAsync(unknownRoute, null, testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    // Already cancelled registration returns 409 Conflict
    // Given a registration that has already been cancelled
    // When the attendee self-cancels the registration again
    // Then the API returns 409 Conflict
    [TestMethod]
    public async Task SelfCancelRegistration_AlreadyCancelled_Returns409()
    {
        var fixture = SelfCancelRegistrationFixture.WithCancelledRegistration();
        await fixture.SetupAsync(Environment, alreadyCancelled: true);

        using var client = Environment.CreatePartnerApiClient(fixture.ApiKey);
        var response = await client.PostAsync(fixture.CancelRoute, null, testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }
}
