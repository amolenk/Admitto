using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Amolenk.Admitto.Api.Tests.Infrastructure;
using Shouldly;

namespace Amolenk.Admitto.Api.Tests.Registrations.OtpVerify;

[TestClass]
public sealed class OtpVerifyTests(TestContext testContext) : EndToEndTestBase
{
    // SC006: Successful OTP verification returns 200 with token
    [TestMethod]
    public async Task SC006_VerifyOtp_CorrectCode_Returns200WithToken()
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

    // SC007: Wrong OTP code returns 400 and increments failed attempts
    [TestMethod]
    public async Task SC007_VerifyOtp_WrongCode_Returns400()
    {
        var fixture = OtpVerifyFixture.WithActiveCode();
        await fixture.SetupAsync(Environment);
        await fixture.SeedValidCodeAsync(Environment);

        using var client = Environment.CreatePublicApiClient(fixture.ApiKey);
        var response = await client.PostAsJsonAsync(
            fixture.VerifyOtpRoute,
            new { Email = OtpVerifyFixture.AttendeeEmail, Code = "000000" },
            cancellationToken: testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    // SC008: Code locked after 5 failed attempts returns 400
    [TestMethod]
    public async Task SC008_VerifyOtp_FifthFailedAttempt_LocksCode_Returns400()
    {
        var fixture = OtpVerifyFixture.WithActiveCode();
        await fixture.SetupAsync(Environment);
        await fixture.SeedLockedCodeAsync(Environment);

        using var client = Environment.CreatePublicApiClient(fixture.ApiKey);
        // 5th wrong attempt should lock and return 400
        var response = await client.PostAsJsonAsync(
            fixture.VerifyOtpRoute,
            new { Email = OtpVerifyFixture.AttendeeEmail, Code = "000000" },
            cancellationToken: testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    // SC009: Expired code returns 400
    [TestMethod]
    public async Task SC009_VerifyOtp_ExpiredCode_Returns400()
    {
        var fixture = OtpVerifyFixture.WithActiveCode();
        await fixture.SetupAsync(Environment);
        await fixture.SeedExpiredCodeAsync(Environment);

        using var client = Environment.CreatePublicApiClient(fixture.ApiKey);
        var response = await client.PostAsJsonAsync(
            fixture.VerifyOtpRoute,
            new { Email = OtpVerifyFixture.AttendeeEmail, Code = OtpVerifyFixture.KnownPlainCode },
            cancellationToken: testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    // SC010: Already-used code returns 400
    [TestMethod]
    public async Task SC010_VerifyOtp_AlreadyUsedCode_Returns400()
    {
        var fixture = OtpVerifyFixture.WithActiveCode();
        await fixture.SetupAsync(Environment);
        await fixture.SeedUsedCodeAsync(Environment);

        using var client = Environment.CreatePublicApiClient(fixture.ApiKey);
        var response = await client.PostAsJsonAsync(
            fixture.VerifyOtpRoute,
            new { Email = OtpVerifyFixture.AttendeeEmail, Code = OtpVerifyFixture.KnownPlainCode },
            cancellationToken: testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    // SC011: No code exists for email+event returns 400
    [TestMethod]
    public async Task SC011_VerifyOtp_NoCodeForEmail_Returns400()
    {
        var fixture = OtpVerifyFixture.WithActiveCode();
        await fixture.SetupAsync(Environment);

        using var client = Environment.CreatePublicApiClient(fixture.ApiKey);
        var response = await client.PostAsJsonAsync(
            fixture.VerifyOtpRoute,
            new { Email = "nobody@example.com", Code = "123456" },
            cancellationToken: testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }
}
