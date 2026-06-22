using System.Net;
using System.Net.Http.Json;
using Amolenk.Admitto.Api.Tests.Infrastructure;
using Shouldly;

namespace Amolenk.Admitto.Api.Tests.Registrations.OtpRequest;

[TestClass]
public sealed class OtpRequestTests(TestContext testContext) : EndToEndTestBase
{
    // Successful OTP request returns 202 Accepted
    [TestMethod]
    public async Task RequestOtp_ValidEmail_Returns202()
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

    // Unknown email still returns 202 (no enumeration)
    [TestMethod]
    public async Task RequestOtp_UnknownEmail_Returns202()
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

    // Second request supersedes previous pending code and returns 202
    [TestMethod]
    public async Task RequestOtp_SupersedesPreviousCode_Returns202()
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

     // Rate limit exceeded returns 429 (TooManyRequests)
    [TestMethod]
    public async Task RequestOtp_RateLimitExceeded_Returns429()
    {
        var fixture = OtpRequestFixture.ActiveEvent();
        await fixture.SetupAsync(Environment);
        await fixture.SeedRateLimitedCodesAsync(Environment, OtpRequestFixture.AttendeeEmail);

        using var client = Environment.CreatePublicApiClient(fixture.ApiKey);
        var response = await client.PostAsJsonAsync(
            fixture.RequestOtpRoute,
            new { Email = OtpRequestFixture.AttendeeEmail },
            cancellationToken: testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.TooManyRequests);
    }

    // Unknown event slug returns 404
    [TestMethod]
    public async Task RequestOtp_UnknownEvent_Returns404()
    {
        var fixture = OtpRequestFixture.ActiveEvent();
        await fixture.SetupAsync(Environment);

        using var client = Environment.CreatePublicApiClient(fixture.ApiKey);
        var unknownRoute = $"/api/events/{Guid.NewGuid()}/otp/request";
        var response = await client.PostAsJsonAsync(
            unknownRoute,
            new { Email = OtpRequestFixture.AttendeeEmail },
            cancellationToken: testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }
}
