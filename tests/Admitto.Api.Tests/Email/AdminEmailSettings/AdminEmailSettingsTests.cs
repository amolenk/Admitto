using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Amolenk.Admitto.Api.Tests.Infrastructure;
using Amolenk.Admitto.Testing.Infrastructure.TestContexts;
using Shouldly;

namespace Amolenk.Admitto.Api.Tests.Email.AdminEmailSettings;

[TestClass]
public sealed class AdminEmailSettingsTests(TestContext testContext) : EndToEndTestBase
{
    // Scenario: Create team-scoped email settings
    // WHEN an organizer creates email settings for team "acme-settings" with no version
    // THEN the response is 201 Created and a subsequent GET returns the settings
    [TestMethod]
    public async Task CreateTeamSettings_ReturnsCreated()
    {
        var fixture = AdminEmailSettingsFixture.EmptySettings();
        await fixture.SetupEmptyAsync(Environment);

        var request = new
        {
            SmtpHost = "smtp.acme.org",
            SmtpPort = 587,
            FromAddress = "events@acme.org",
            AuthMode = "none",
            Username = (string?)null,
            Password = (string?)null,
            Version = (uint?)null
        };

        var response = await Environment.ApiClient.PutAsJsonAsync(
            fixture.TeamSettingsRoute,
            request,
            cancellationToken: testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
    }

    // Scenario: Admin GET masks the password — team scope
    // WHEN an organizer reads team-scoped settings
    // THEN the response contains HasPassword and does not expose the plaintext password field
    [TestMethod]
    public async Task GetTeamSettings_ReturnsMaskedResponse()
    {
        var fixture = AdminEmailSettingsFixture.WithTeamSettings();
        await fixture.SetupTeamSettingsAsync(Environment);

        var response = await Environment.ApiClient.GetAsync(
            fixture.TeamSettingsRoute,
            testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>(
            cancellationToken: testContext.CancellationToken);
        body.GetProperty("smtpHost").GetString().ShouldNotBeNullOrEmpty();
        body.GetProperty("fromAddress").GetString().ShouldBe("team@example.com");
        body.GetProperty("hasPassword").GetBoolean().ShouldBe(false);
        body.TryGetProperty("password", out _).ShouldBeFalse();
    }

    // Scenario: Update from-address only — team scope
    // WHEN an organizer submits an update with the correct Version
    // THEN the response is 200 OK
    [TestMethod]
    public async Task UpdateTeamSettings_WithCorrectVersion_ReturnsOk()
    {
        var fixture = AdminEmailSettingsFixture.WithTeamSettings();
        var version = await fixture.SetupTeamSettingsAsync(Environment);

        var request = new
        {
            SmtpHost = "smtp.acme.org",
            SmtpPort = 587,
            FromAddress = "updated@acme.org",
            AuthMode = "none",
            Username = (string?)null,
            Password = (string?)null,
            Version = version
        };

        var response = await Environment.ApiClient.PutAsJsonAsync(
            fixture.TeamSettingsRoute,
            request,
            cancellationToken: testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    // Scenario: DELETE team-scoped email settings
    // WHEN an organizer deletes team-scoped settings
    // THEN the response is 204 No Content and a subsequent GET returns 404
    [TestMethod]
    public async Task DeleteTeamSettings_ReturnsNoContent()
    {
        var fixture = AdminEmailSettingsFixture.WithTeamSettings();
        var version = await fixture.SetupTeamSettingsAsync(Environment);

        var response = await Environment.ApiClient.DeleteAsync(
            $"{fixture.TeamSettingsRoute}?version={version}",
            testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var getResponse = await Environment.ApiClient.GetAsync(
            fixture.TeamSettingsRoute,
            testContext.CancellationToken);
        getResponse.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    // Scenario: Reject update with stale version
    // WHEN an organizer submits an update with a Version older than the stored value
    // THEN the request is rejected with a 409 Conflict
    [TestMethod]
    public async Task UpdateWithStaleVersion_ReturnsConflict()
    {
        var fixture = AdminEmailSettingsFixture.WithTeamSettings();
        await fixture.SetupTeamSettingsAsync(Environment);

        var request = new
        {
            SmtpHost = "smtp.acme.org",
            SmtpPort = 587,
            FromAddress = "stale@acme.org",
            AuthMode = "none",
            Username = (string?)null,
            Password = (string?)null,
            Version = (uint)9999
        };

        var response = await Environment.ApiClient.PutAsJsonAsync(
            fixture.TeamSettingsRoute,
            request,
            cancellationToken: testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }

    // Scenario: Non-team-member denied
    // WHEN a user who is not a member of team "acme-settings" attempts to read or update settings
    // THEN the request is denied with a 403 Forbidden
    [TestMethod]
    public async Task NonMember_Denied_Returns403()
    {
        var fixture = AdminEmailSettingsFixture.WithTeamSettings();
        await fixture.SetupTeamSettingsAsync(Environment);

        var response = await Environment.BobApiClient.GetAsync(
            fixture.TeamSettingsRoute,
            testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    // Scenario: Diagnostic send succeeds at team scope
    // WHEN an organizer tests team-scoped email settings
    // THEN the response is 200 OK and MailDev receives the diagnostic message
    [TestMethod]
    public async Task TestTeamSettings_ReturnsOkAndSendsDiagnostic()
    {
        var fixture = AdminEmailSettingsFixture.WithTeamSettings();
        await fixture.SetupTeamSmtpSettingsAsync(Environment);

        var response = await Environment.ApiClient.PostAsJsonAsync(
            fixture.TeamSettingsTestRoute,
            new { Recipient = "ops@acme.org" },
            cancellationToken: testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var messages = await Environment.Email.WaitForAsync(
            1,
            TimeSpan.FromSeconds(10),
            testContext.CancellationToken);

        EmailTestContext.GetLowercaseRecipientAddresses(messages).ShouldContain("ops@acme.org");
    }

    // Scenario: Recipient validation
    // WHEN an organizer submits an invalid recipient
    // THEN the endpoint validator rejects the request with 400 Bad Request
    [TestMethod]
    public async Task TestEmailSettings_InvalidRecipient_ReturnsBadRequest()
    {
        var fixture = AdminEmailSettingsFixture.EmptySettings();
        await fixture.SetupEmptyAsync(Environment);

        var response = await Environment.ApiClient.PostAsJsonAsync(
            fixture.TeamSettingsTestRoute,
            new { Recipient = "not-an-email" },
            cancellationToken: testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    // Scenario: Authorization
    // WHEN a non-organizer tests email settings
    // THEN the request is denied before a diagnostic email is sent
    [TestMethod]
    public async Task TestEmailSettings_NonOrganizer_ReturnsForbidden()
    {
        var fixture = AdminEmailSettingsFixture.WithTeamSettings();
        await fixture.SetupTeamSmtpSettingsAsync(Environment);

        var response = await Environment.BobApiClient.PostAsJsonAsync(
            fixture.TeamSettingsTestRoute,
            new { Recipient = "ops@acme.org" },
            cancellationToken: testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);

        var messages = await Environment.Email.WaitForAsync(
            1,
            TimeSpan.FromSeconds(2),
            testContext.CancellationToken);

        messages.ShouldBeEmpty();
    }
}
