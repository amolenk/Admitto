using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Amolenk.Admitto.Api.Tests.Infrastructure;
using Shouldly;

namespace Amolenk.Admitto.Api.Tests.Registrations.OtpVerify;

[TestClass]
public sealed class OtpVerifyTests(TestContext testContext) : EndToEndTestBase
{
    // Successful OTP verification returns 200 with token
    [TestMethod]
    public async Task VerifyOtp_CorrectCode_Returns200WithToken()
    {
        var fixture = OtpVerifyFixture.WithActiveCode();
        await fixture.SetupAsync(Environment);
        await fixture.SeedValidCodeAsync(Environment);

        using var client = Environment.CreatePublicApiClient(fixture.ApiKey);
        var response = await client.PostAsJsonAsync(
            fixture.VerifyOtpRoute,
            new { Email = OtpVerifyFixture.AttendeeEmail, Code = OtpVerifyFixture.KnownPlainCode },
            cancellationToken: testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>(
            cancellationToken: testContext.CancellationToken);
        body.TryGetProperty("token", out var tokenProp).ShouldBeTrue();
        tokenProp.GetString().ShouldNotBeNullOrEmpty();
    }

    // Wrong OTP code returns 422 and increments failed attempts
    [TestMethod]
    public async Task VerifyOtp_WrongCode_Returns422()
    {
        var fixture = OtpVerifyFixture.WithActiveCode();
        await fixture.SetupAsync(Environment);
        await fixture.SeedValidCodeAsync(Environment);

        using var client = Environment.CreatePublicApiClient(fixture.ApiKey);
        var response = await client.PostAsJsonAsync(
            fixture.VerifyOtpRoute,
            new { Email = OtpVerifyFixture.AttendeeEmail, Code = "000000" },
            cancellationToken: testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
    }

    // Code locked after 5 failed attempts returns 422
    [TestMethod]
    public async Task VerifyOtp_FifthFailedAttempt_LocksCode_Returns422()
    {
        var fixture = OtpVerifyFixture.WithActiveCode();
        await fixture.SetupAsync(Environment);
        await fixture.SeedLockedCodeAsync(Environment);

        using var client = Environment.CreatePublicApiClient(fixture.ApiKey);
        // 5th wrong attempt should lock and return 422
        var response = await client.PostAsJsonAsync(
            fixture.VerifyOtpRoute,
            new { Email = OtpVerifyFixture.AttendeeEmail, Code = "000000" },
            cancellationToken: testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
    }

    // Expired code returns 422
    [TestMethod]
    public async Task VerifyOtp_ExpiredCode_Returns422()
    {
        var fixture = OtpVerifyFixture.WithActiveCode();
        await fixture.SetupAsync(Environment);
        await fixture.SeedExpiredCodeAsync(Environment);

        using var client = Environment.CreatePublicApiClient(fixture.ApiKey);
        var response = await client.PostAsJsonAsync(
            fixture.VerifyOtpRoute,
            new { Email = OtpVerifyFixture.AttendeeEmail, Code = OtpVerifyFixture.KnownPlainCode },
            cancellationToken: testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
    }

    // Already-used code returns 422
    [TestMethod]
    public async Task VerifyOtp_AlreadyUsedCode_Returns422()
    {
        var fixture = OtpVerifyFixture.WithActiveCode();
        await fixture.SetupAsync(Environment);
        await fixture.SeedUsedCodeAsync(Environment);

        using var client = Environment.CreatePublicApiClient(fixture.ApiKey);
        var response = await client.PostAsJsonAsync(
            fixture.VerifyOtpRoute,
            new { Email = OtpVerifyFixture.AttendeeEmail, Code = OtpVerifyFixture.KnownPlainCode },
            cancellationToken: testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
    }

    // No code exists for email+event returns 422
    [TestMethod]
    public async Task VerifyOtp_NoCodeForEmail_Returns422()
    {
        var fixture = OtpVerifyFixture.WithActiveCode();
        await fixture.SetupAsync(Environment);

        using var client = Environment.CreatePublicApiClient(fixture.ApiKey);
        var response = await client.PostAsJsonAsync(
            fixture.VerifyOtpRoute,
            new { Email = "nobody@example.com", Code = "123456" },
            cancellationToken: testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
    }
}
