using System.Net;
using Amolenk.Admitto.Api.Tests.Infrastructure;
using Shouldly;

namespace Amolenk.Admitto.Api.Tests.Registrations.ReconfirmRegistration;

[TestClass]
public sealed class ReconfirmRegistrationTests(TestContext testContext) : EndToEndTestBase
{
    [TestMethod]
    public async Task ReconfirmRegistration_ExistingRegistration_Returns204()
    {
        var fixture = ReconfirmRegistrationFixture.HappyFlow();
        await fixture.SetupAsync(Environment);

        using var client = Environment.CreatePartnerApiClient(fixture.ApiKey);
        var response = await client.PostAsync(
            fixture.ReconfirmRoute(fixture.RegistrationId.Value), null, testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }

    [TestMethod]
    public async Task ReconfirmRegistration_CalledTwice_IsIdempotent()
    {
        var fixture = ReconfirmRegistrationFixture.HappyFlow();
        await fixture.SetupAsync(Environment);

        using var client = Environment.CreatePartnerApiClient(fixture.ApiKey);
        var first = await client.PostAsync(
            fixture.ReconfirmRoute(fixture.RegistrationId.Value), null, testContext.CancellationToken);
        var second = await client.PostAsync(
            fixture.ReconfirmRoute(fixture.RegistrationId.Value), null, testContext.CancellationToken);

        first.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        second.StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }

    [TestMethod]
    public async Task ReconfirmRegistration_MissingApiKey_Returns401()
    {
        var fixture = ReconfirmRegistrationFixture.HappyFlow();
        await fixture.SetupAsync(Environment);

        using var client = new HttpClient { BaseAddress = Environment.ApiClient.BaseAddress };
        var response = await client.PostAsync(
            fixture.ReconfirmRoute(fixture.RegistrationId.Value), null, testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [TestMethod]
    public async Task ReconfirmRegistration_UnknownRegistration_Returns404()
    {
        var fixture = ReconfirmRegistrationFixture.WithoutRegistration();
        await fixture.SetupAsync(Environment);

        using var client = Environment.CreatePartnerApiClient(fixture.ApiKey);
        var response = await client.PostAsync(
            fixture.ReconfirmRoute(Guid.NewGuid()), null, testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [TestMethod]
    public async Task ReconfirmRegistration_CancelledRegistration_Returns409()
    {
        var fixture = ReconfirmRegistrationFixture.WithCancelledRegistration();
        await fixture.SetupAsync(Environment);

        using var client = Environment.CreatePartnerApiClient(fixture.ApiKey);
        var response = await client.PostAsync(
            fixture.ReconfirmRoute(fixture.RegistrationId.Value), null, testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }
}
