using System.Net;
using Amolenk.Admitto.Api.Tests.Infrastructure;
using Shouldly;

namespace Amolenk.Admitto.Api.Tests.Registrations.GetQRCode;

[TestClass]
public sealed class GetQRCodeTests(TestContext testContext) : EndToEndTestBase
{
    [TestMethod]
    public async Task ExistingRegistration_Returns200WithExpectedPng()
    {
        var fixture = GetQRCodeFixture.HappyFlow();
        await fixture.SetupAsync(Environment);

        using var client = Environment.CreatePublicApiClient(fixture.ApiKey);
        var response = await client.GetAsync(
            fixture.Route(fixture.RegistrationId),
            testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.ShouldBe("image/png");

        var body = await response.Content.ReadAsByteArrayAsync(testContext.CancellationToken);
        body.ShouldNotBeEmpty();

        var expected = GetQRCodeFixture.GenerateExpectedQRCode(fixture.RegistrationId);
        body.ShouldBe(expected);
    }

    [TestMethod]
    public async Task MissingApiKey_Returns401()
    {
        var fixture = GetQRCodeFixture.HappyFlow();
        await fixture.SetupAsync(Environment);

        using var bareClient = new HttpClient { BaseAddress = Environment.ApiClient.BaseAddress };
        var response = await bareClient.GetAsync(
            fixture.Route(fixture.RegistrationId), testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [TestMethod]
    public async Task UnknownEvent_Returns404()
    {
        var fixture = GetQRCodeFixture.HappyFlow();
        await fixture.SetupAsync(Environment);

        using var client = Environment.CreatePublicApiClient(fixture.ApiKey);
        var response = await client.GetAsync(
            fixture.Route(fixture.RegistrationId, eventId: Guid.NewGuid()),
            testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [TestMethod]
    public async Task UnknownRegistration_Returns404()
    {
        var fixture = GetQRCodeFixture.WithoutRegistration();
        await fixture.SetupAsync(Environment);

        using var client = Environment.CreatePublicApiClient(fixture.ApiKey);
        var unknownId = Guid.NewGuid();
        var response = await client.GetAsync(
            fixture.Route(unknownId),
            testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [TestMethod]
    public async Task CancelledRegistration_Returns200()
    {
        var fixture = GetQRCodeFixture.WithCancelledRegistration();
        await fixture.SetupAsync(Environment);

        using var client = Environment.CreatePublicApiClient(fixture.ApiKey);
        var response = await client.GetAsync(
            fixture.Route(fixture.RegistrationId),
            testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.ShouldBe("image/png");

        var body = await response.Content.ReadAsByteArrayAsync(testContext.CancellationToken);
        body.ShouldNotBeEmpty();
    }
}
