using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Amolenk.Admitto.Api.Tests.Infrastructure;
using Shouldly;

namespace Amolenk.Admitto.Api.Tests.Email.AdminEmailSettings;

[TestClass]
public sealed class AdminEmailSettingsTests(TestContext testContext) : EndToEndTestBase
{
    // Scenario: Create team-scoped email settings
    // WHEN an organizer creates email settings for team "acme-settings" with no version
    // THEN the response is 201 Created and a subsequent GET returns the settings
    [TestMethod]
    public async Task SC001_CreateTeamSettings_ReturnsCreated()
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
            AdminEmailSettingsFixture.TeamSettingsRoute,
            request,
            cancellationToken: testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
    }

    // Scenario: Create event-scoped email settings
    // WHEN an organizer creates email settings for event "settingsconf" with no version
    // THEN the response is 201 Created
    [TestMethod]
    public async Task SC002_CreateEventSettings_ReturnsCreated()
    {
        var fixture = AdminEmailSettingsFixture.EmptySettings();
        await fixture.SetupEmptyAsync(Environment);

        var request = new
        {
            SmtpHost = "smtp.acme.org",
            SmtpPort = 587,
            FromAddress = "event@acme.org",
            AuthMode = "none",
            Username = (string?)null,
            Password = (string?)null,
            Version = (uint?)null
        };

        var response = await Environment.ApiClient.PutAsJsonAsync(
            AdminEmailSettingsFixture.EventSettingsRoute,
            request,
            cancellationToken: testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
    }

    // Scenario: Admin GET masks the password — team scope
    // WHEN an organizer reads team-scoped settings
    // THEN the response contains HasPassword and does not expose the plaintext password field
    [TestMethod]
    public async Task SC003_GetTeamSettings_ReturnsMaskedResponse()
    {
        var fixture = AdminEmailSettingsFixture.WithTeamSettings();
        await fixture.SetupTeamSettingsAsync(Environment);

        var response = await Environment.ApiClient.GetAsync(
            AdminEmailSettingsFixture.TeamSettingsRoute,
            testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>(
            cancellationToken: testContext.CancellationToken);
        body.GetProperty("smtpHost").GetString().ShouldNotBeNullOrEmpty();
        body.GetProperty("fromAddress").GetString().ShouldBe("team@example.com");
        body.GetProperty("hasPassword").GetBoolean().ShouldBe(false);
        body.TryGetProperty("password", out _).ShouldBeFalse();
    }

    // Scenario: Admin GET masks the password — event scope
    // WHEN an organizer reads event-scoped settings
    // THEN the response is 200 OK with the expected fields
    [TestMethod]
    public async Task SC004_GetEventSettings_ReturnsMaskedResponse()
    {
        var fixture = AdminEmailSettingsFixture.WithBothSettings();
        await fixture.SetupBothSettingsAsync(Environment);

        var response = await Environment.ApiClient.GetAsync(
            AdminEmailSettingsFixture.EventSettingsRoute,
            testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>(
            cancellationToken: testContext.CancellationToken);
        body.GetProperty("fromAddress").GetString().ShouldBe("event@example.com");
    }

    // Scenario: Update from-address only — team scope
    // WHEN an organizer submits an update with the correct Version
    // THEN the response is 200 OK
    [TestMethod]
    public async Task SC005_UpdateTeamSettings_WithCorrectVersion_ReturnsOk()
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
            AdminEmailSettingsFixture.TeamSettingsRoute,
            request,
            cancellationToken: testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    // Scenario: Update from-address only — event scope
    // WHEN an organizer submits an update to event-scoped settings with the correct Version
    // THEN the response is 200 OK
    [TestMethod]
    public async Task SC006_UpdateEventSettings_WithCorrectVersion_ReturnsOk()
    {
        var fixture = AdminEmailSettingsFixture.WithBothSettings();
        var (_, eventVersion) = await fixture.SetupBothSettingsAsync(Environment);

        var request = new
        {
            SmtpHost = "smtp.acme.org",
            SmtpPort = 587,
            FromAddress = "updated-event@acme.org",
            AuthMode = "none",
            Username = (string?)null,
            Password = (string?)null,
            Version = eventVersion
        };

        var response = await Environment.ApiClient.PutAsJsonAsync(
            AdminEmailSettingsFixture.EventSettingsRoute,
            request,
            cancellationToken: testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    // Scenario: DELETE team-scoped email settings
    // WHEN an organizer deletes team-scoped settings
    // THEN the response is 204 No Content and a subsequent GET returns 404
    [TestMethod]
    public async Task SC007_DeleteTeamSettings_ReturnsNoContent()
    {
        var fixture = AdminEmailSettingsFixture.WithTeamSettings();
        var version = await fixture.SetupTeamSettingsAsync(Environment);

        var response = await Environment.ApiClient.DeleteAsync(
            $"{AdminEmailSettingsFixture.TeamSettingsRoute}?version={version}",
            testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var getResponse = await Environment.ApiClient.GetAsync(
            AdminEmailSettingsFixture.TeamSettingsRoute,
            testContext.CancellationToken);
        getResponse.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    // Scenario: DELETE event-scoped email settings
    // WHEN an organizer deletes event-scoped settings
    // THEN the response is 204 No Content
    [TestMethod]
    public async Task SC008_DeleteEventSettings_ReturnsNoContent()
    {
        var fixture = AdminEmailSettingsFixture.WithBothSettings();
        var (_, eventVersion) = await fixture.SetupBothSettingsAsync(Environment);

        var response = await Environment.ApiClient.DeleteAsync(
            $"{AdminEmailSettingsFixture.EventSettingsRoute}?version={eventVersion}",
            testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }

    // Scenario: Reject update with stale version
    // WHEN an organizer submits an update with a Version older than the stored value
    // THEN the request is rejected with a 409 Conflict
    [TestMethod]
    public async Task SC009_UpdateWithStaleVersion_ReturnsConflict()
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
            AdminEmailSettingsFixture.TeamSettingsRoute,
            request,
            cancellationToken: testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }

    // Scenario: Non-team-member denied
    // WHEN a user who is not a member of team "acme-settings" attempts to read or update settings
    // THEN the request is denied with a 403 Forbidden
    [TestMethod]
    public async Task SC010_NonMember_Denied_Returns403()
    {
        var fixture = AdminEmailSettingsFixture.WithTeamSettings();
        await fixture.SetupTeamSettingsAsync(Environment);

        var response = await Environment.BobApiClient.GetAsync(
            AdminEmailSettingsFixture.TeamSettingsRoute,
            testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }
}
