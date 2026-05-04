using System.Net;
using Amolenk.Admitto.Api.Tests.Infrastructure;
using Shouldly;

namespace Amolenk.Admitto.Api.Tests.Registrations.GetQRCode;

[TestClass]
public sealed class GetQRCodeTests(TestContext testContext) : EndToEndTestBase
{
    [TestMethod]
    public async Task SC001_ValidSignature_Returns200WithExpectedPng()
    {
        var fixture = GetQRCodeFixture.HappyFlow();
        await fixture.SetupAsync(Environment);

        using var client = Environment.CreatePublicApiClient(fixture.ApiKey);
        var signature = fixture.ValidSignature;
        var response = await client.GetAsync(
            GetQRCodeFixture.Route(fixture.RegistrationId, signature),
            testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.ShouldBe("image/png");

        var body = await response.Content.ReadAsByteArrayAsync(testContext.CancellationToken);
        body.ShouldNotBeEmpty();

        var expected = GetQRCodeFixture.GenerateExpectedQRCode(fixture.RegistrationId, signature);
        body.ShouldBe(expected);
    }

    [TestMethod]
    public async Task SC002_InvalidSignature_Returns403()
    {
        // Endpoint construction guarantees no Registration row is read when the signature check
        // fails (verify-before-load order). End-to-end coverage of "no row read" is asserted
        // structurally by GetQRCodeHttpEndpoint; this test verifies the externally observable 403.
        var fixture = GetQRCodeFixture.HappyFlow();
        await fixture.SetupAsync(Environment);

        using var client = Environment.CreatePublicApiClient(fixture.ApiKey);
        var bogus = GetQRCodeFixture.Sign(Guid.NewGuid(), fixture.SigningKeyBase64);
        var response = await client.GetAsync(
            GetQRCodeFixture.Route(fixture.RegistrationId, bogus),
            testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [TestMethod]
    public async Task SC003_MissingSignature_Returns403()
    {
        var fixture = GetQRCodeFixture.HappyFlow();
        await fixture.SetupAsync(Environment);

        using var client = Environment.CreatePublicApiClient(fixture.ApiKey);
        var response = await client.GetAsync(
            GetQRCodeFixture.Route(fixture.RegistrationId, signature: null),
            testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [TestMethod]
    public async Task SC004a_UnknownTeamSlug_Returns404()
    {
        var fixture = GetQRCodeFixture.HappyFlow();
        await fixture.SetupAsync(Environment);

        using var client = Environment.CreatePublicApiClient(fixture.ApiKey);
        var response = await client.GetAsync(
            GetQRCodeFixture.Route(
                fixture.RegistrationId, fixture.ValidSignature, teamSlug: "ghost-team"),
            testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [TestMethod]
    public async Task SC004b_UnknownEventSlug_Returns404()
    {
        var fixture = GetQRCodeFixture.HappyFlow();
        await fixture.SetupAsync(Environment);

        using var client = Environment.CreatePublicApiClient(fixture.ApiKey);
        var response = await client.GetAsync(
            GetQRCodeFixture.Route(
                fixture.RegistrationId, fixture.ValidSignature, eventSlug: "ghost-event"),
            testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [TestMethod]
    public async Task SC005_ValidSignatureUnknownRegistration_Returns404()
    {
        var fixture = GetQRCodeFixture.WithoutRegistration();
        await fixture.SetupAsync(Environment);

        using var client = Environment.CreatePublicApiClient(fixture.ApiKey);
        var unknownId = Guid.NewGuid();
        var signature = GetQRCodeFixture.Sign(unknownId, fixture.SigningKeyBase64);
        var response = await client.GetAsync(
            GetQRCodeFixture.Route(unknownId, signature),
            testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [TestMethod]
    public async Task SC006_SignatureFromOtherEvent_Returns403()
    {
        var fixture = GetQRCodeFixture.WithSecondEvent();
        await fixture.SetupAsync(Environment);

        using var client = Environment.CreatePublicApiClient(fixture.ApiKey);
        // Sign the registration id under the OTHER event's key, then use the primary event's path.
        var crossEventSignature = GetQRCodeFixture.Sign(
            fixture.RegistrationId, fixture.OtherEventSigningKeyBase64);
        var response = await client.GetAsync(
            GetQRCodeFixture.Route(fixture.RegistrationId, crossEventSignature),
            testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [TestMethod]
    public async Task SC007_CancelledRegistration_Returns200()
    {
        var fixture = GetQRCodeFixture.WithCancelledRegistration();
        await fixture.SetupAsync(Environment);

        using var client = Environment.CreatePublicApiClient(fixture.ApiKey);
        var response = await client.GetAsync(
            GetQRCodeFixture.Route(fixture.RegistrationId, fixture.ValidSignature),
            testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.ShouldBe("image/png");

        var body = await response.Content.ReadAsByteArrayAsync(testContext.CancellationToken);
        body.ShouldNotBeEmpty();
    }
}
