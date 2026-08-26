using System.Net;
using Amolenk.Admitto.Api.Tests.Infrastructure;
using Shouldly;

namespace Amolenk.Admitto.Api.Tests.Registrations.GetQRCode;

[TestClass]
public sealed class GetQRCodeTests(TestContext testContext) : EndToEndTestBase
{
    // Given an existing registration
    // When its QR code is requested
    // Then it returns 200 OK with the expected PNG image
    [TestMethod]
    public async Task ExistingRegistration_Returns200WithExpectedPng()
    {
        var fixture = GetQRCodeFixture.HappyFlow();
        await fixture.SetupAsync(Environment);

        using var client = new HttpClient { BaseAddress = Environment.ApiClient.BaseAddress };
        var response = await client.GetAsync(
            fixture.QRCodeRoute(fixture.RegistrationId),
            testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.ShouldBe("image/png");
        response.Content.Headers.ContentDisposition?.FileName?.Trim('"').ShouldBe("qrcode.png");

        var body = await response.Content.ReadAsByteArrayAsync(testContext.CancellationToken);
        body.ShouldNotBeEmpty();

        var expected = GetQRCodeFixture.GenerateExpectedQRCode(fixture.RegistrationId);
        body.ShouldBe(expected);
    }

    // Given an existing registration
    // When its QR code is requested without an API key
    // Then it still returns 200 OK
    [TestMethod]
    public async Task MissingApiKey_StillReturns200()
    {
        var fixture = GetQRCodeFixture.HappyFlow();
        await fixture.SetupAsync(Environment);

        using var bareClient = new HttpClient { BaseAddress = Environment.ApiClient.BaseAddress };
        var response = await bareClient.GetAsync(
            fixture.QRCodeRoute(fixture.RegistrationId), testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    // Given an existing registration
    // When its QR code is requested for an unknown event slug
    // Then it returns 404 Not Found
    [TestMethod]
    public async Task UnknownEvent_Returns404()
    {
        var fixture = GetQRCodeFixture.HappyFlow();
        await fixture.SetupAsync(Environment);

        using var client = new HttpClient { BaseAddress = Environment.ApiClient.BaseAddress };
        var response = await client.GetAsync(
            fixture.QRCodeRoute(fixture.RegistrationId, publicSlug: "unknown-event"),
            testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    // Given an event with no matching registration
    // When the QR code is requested for an unknown registration id
    // Then it returns 404 Not Found
    [TestMethod]
    public async Task UnknownRegistration_Returns404()
    {
        var fixture = GetQRCodeFixture.WithoutRegistration();
        await fixture.SetupAsync(Environment);

        using var client = new HttpClient { BaseAddress = Environment.ApiClient.BaseAddress };
        var unknownId = Guid.NewGuid();
        var response = await client.GetAsync(
            fixture.QRCodeRoute(unknownId),
            testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    // Given a cancelled registration
    // When its QR code is requested
    // Then it still returns 200 OK with a PNG image
    [TestMethod]
    public async Task CancelledRegistration_Returns200()
    {
        var fixture = GetQRCodeFixture.WithCancelledRegistration();
        await fixture.SetupAsync(Environment);

        using var client = new HttpClient { BaseAddress = Environment.ApiClient.BaseAddress };
        var response = await client.GetAsync(
            fixture.QRCodeRoute(fixture.RegistrationId),
            testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.ShouldBe("image/png");

        var body = await response.Content.ReadAsByteArrayAsync(testContext.CancellationToken);
        body.ShouldNotBeEmpty();
    }

    // Given an existing registration
    // When its QR code is requested via the old partner API route
    // Then the response does not serve a QR code image
    [TestMethod]
    public async Task OldPartnerApiRoute_DoesNotServeQRCodeImage()
    {
        var fixture = GetQRCodeFixture.HappyFlow();
        await fixture.SetupAsync(Environment);

        using var client = Environment.CreatePartnerApiClient(fixture.ApiKey);
        var response = await client.GetAsync(
            fixture.OldPartnerQRCodeRoute(fixture.RegistrationId),
            testContext.CancellationToken);

        response.StatusCode.ShouldNotBe(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.ShouldNotBe("image/png");
    }
}
