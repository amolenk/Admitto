using System.Net;
using System.Net.Http.Json;
using Amolenk.Admitto.Api.Tests.Infrastructure;
using Shouldly;

namespace Amolenk.Admitto.Api.Tests.Registrations.OtpRequest;

[TestClass]
public sealed class OtpRequestTests(TestContext testContext) : EndToEndTestBase
{
    // SC001: Successful OTP request returns 202 Accepted
    [TestMethod]
    public async Task SC001_RequestOtp_ValidEmail_Returns202()
    {
        var fixture = OtpRequestFixture.ActiveEvent();
        await fixture.SetupAsync(Environment);

        using var client = Environment.CreatePublicApiClient(fixture.ApiKey);
        var response = await client.PostAsJsonAsync(
            fixture.RequestOtpRoute,
            new { Email = OtpRequestFixture.AttendeeEmail },
            cancellationToken: testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Accepted);
    }

    // SC002: Unknown email still returns 202 (no enumeration)
    [TestMethod]
    public async Task SC002_RequestOtp_UnknownEmail_Returns202()
    {
        var fixture = OtpRequestFixture.ActiveEvent();
        await fixture.SetupAsync(Environment);

        using var client = Environment.CreatePublicApiClient(fixture.ApiKey);
        var response = await client.PostAsJsonAsync(
            fixture.RequestOtpRoute,
            new { Email = "nobody@example.com" },
            cancellationToken: testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Accepted);
    }

    // SC003: Second request supersedes previous pending code and returns 202
    [TestMethod]
    public async Task SC003_RequestOtp_SupersedesPreviousCode_Returns202()
    {
        var fixture = OtpRequestFixture.ActiveEvent();
        await fixture.SetupAsync(Environment);

        var request = new { Email = OtpRequestFixture.AttendeeEmail };

        using var client = Environment.CreatePublicApiClient(fixture.ApiKey);
        var firstResponse = await client.PostAsJsonAsync(
            fixture.RequestOtpRoute, request, cancellationToken: testContext.CancellationToken);
        firstResponse.StatusCode.ShouldBe(HttpStatusCode.Accepted);

        var secondResponse = await client.PostAsJsonAsync(
            fixture.RequestOtpRoute, request, cancellationToken: testContext.CancellationToken);
        secondResponse.StatusCode.ShouldBe(HttpStatusCode.Accepted);
    }

    // SC004: Rate limit exceeded returns 400 (TooManyRequests maps to Validation → 400)
    [TestMethod]
    public async Task SC004_RequestOtp_RateLimitExceeded_Returns400()
    {
        var fixture = OtpRequestFixture.ActiveEvent();
        await fixture.SetupAsync(Environment);
        await fixture.SeedRateLimitedCodesAsync(Environment, OtpRequestFixture.AttendeeEmail);

        using var client = Environment.CreatePublicApiClient(fixture.ApiKey);
        var response = await client.PostAsJsonAsync(
            fixture.RequestOtpRoute,
            new { Email = OtpRequestFixture.AttendeeEmail },
            cancellationToken: testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    // SC005: Unknown event slug returns 404
    [TestMethod]
    public async Task SC005_RequestOtp_UnknownEvent_Returns404()
    {
        var fixture = OtpRequestFixture.ActiveEvent();
        await fixture.SetupAsync(Environment);

        using var client = Environment.CreatePublicApiClient(fixture.ApiKey);
        var unknownRoute = $"/api/teams/{OtpRequestFixture.TeamSlug}/events/nonexistent-event/otp/request";
        var response = await client.PostAsJsonAsync(
            unknownRoute,
            new { Email = OtpRequestFixture.AttendeeEmail },
            cancellationToken: testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }
}
