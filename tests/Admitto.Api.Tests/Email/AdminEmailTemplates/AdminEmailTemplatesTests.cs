using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Amolenk.Admitto.Api.Tests.Infrastructure;
using Shouldly;

namespace Amolenk.Admitto.Api.Tests.Email.AdminEmailTemplates;

[TestClass]
public sealed class AdminEmailTemplatesTests(TestContext testContext) : EndToEndTestBase
{
    // Scenario: Upsert team-scoped template (create)
    // WHEN an organizer creates a ticket template for team "acme-templates" without a Version
    // THEN the response is 201 Created
    [TestMethod]
    public async Task SC001_CreateTeamTemplate_ReturnsCreated()
    {
        var fixture = AdminEmailTemplatesFixture.EmptyTemplates();
        await fixture.SetupEmptyAsync(Environment);

        var request = new
        {
            Subject = "Welcome to {{ event_name }}",
            TextBody = "Hello {{ first_name }}",
            HtmlBody = "<p>Hello {{ first_name }}</p>",
            Version = (uint?)null
        };

        var response = await Environment.ApiClient.PutAsJsonAsync(
            AdminEmailTemplatesFixture.TeamTemplateRoute,
            request,
            cancellationToken: testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
    }

    // Scenario: Upsert event-scoped template (create)
    // WHEN an organizer creates a ticket template for event "templatesconf" without a Version
    // THEN the response is 201 Created
    [TestMethod]
    public async Task SC002_CreateEventTemplate_ReturnsCreated()
    {
        var fixture = AdminEmailTemplatesFixture.EmptyTemplates();
        await fixture.SetupEmptyAsync(Environment);

        var request = new
        {
            Subject = "Event ticket: {{ event_name }}",
            TextBody = "Hi {{ first_name }}, here is your ticket.",
            HtmlBody = "<p>Hi {{ first_name }}, here is your ticket.</p>",
            Version = (uint?)null
        };

        var response = await Environment.ApiClient.PutAsJsonAsync(
            AdminEmailTemplatesFixture.EventTemplateRoute,
            request,
            cancellationToken: testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
    }

    // Scenario: GET team-scoped template
    // WHEN an organizer reads a team-scoped ticket template
    // THEN the response is 200 OK with the stored subject and bodies
    [TestMethod]
    public async Task SC003_GetTeamTemplate_ReturnsOk()
    {
        var fixture = AdminEmailTemplatesFixture.WithTeamTemplate();
        await fixture.SetupTeamTemplateAsync(Environment);

        var response = await Environment.ApiClient.GetAsync(
            AdminEmailTemplatesFixture.TeamTemplateRoute,
            testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>(
            cancellationToken: testContext.CancellationToken);
        body.GetProperty("subject").GetString().ShouldBe("Team subject");
        body.GetProperty("textBody").GetString().ShouldNotBeNullOrEmpty();
        body.GetProperty("htmlBody").GetString().ShouldNotBeNullOrEmpty();
    }

    // Scenario: GET event-scoped template
    // WHEN an organizer reads an event-scoped ticket template
    // THEN the response is 200 OK with the event-scoped subject
    [TestMethod]
    public async Task SC004_GetEventTemplate_ReturnsOk()
    {
        var fixture = AdminEmailTemplatesFixture.WithBothTemplates();
        await fixture.SetupBothTemplatesAsync(Environment);

        var response = await Environment.ApiClient.GetAsync(
            AdminEmailTemplatesFixture.EventTemplateRoute,
            testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>(
            cancellationToken: testContext.CancellationToken);
        body.GetProperty("subject").GetString().ShouldBe("Event subject");
    }

    // Scenario: Upsert team-scoped template (update)
    // WHEN an organizer submits an update with the correct Version
    // THEN the response is 200 OK and a subsequent GET returns the updated content
    [TestMethod]
    public async Task SC005_UpdateTeamTemplate_WithCorrectVersion_ReturnsOk()
    {
        var fixture = AdminEmailTemplatesFixture.WithTeamTemplate();
        var version = await fixture.SetupTeamTemplateAsync(Environment);

        var request = new
        {
            Subject = "Updated subject",
            TextBody = "Updated text",
            HtmlBody = "<p>Updated html</p>",
            Version = version
        };

        var response = await Environment.ApiClient.PutAsJsonAsync(
            AdminEmailTemplatesFixture.TeamTemplateRoute,
            request,
            cancellationToken: testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var getResponse = await Environment.ApiClient.GetAsync(
            AdminEmailTemplatesFixture.TeamTemplateRoute,
            testContext.CancellationToken);
        var body = await getResponse.Content.ReadFromJsonAsync<JsonElement>(
            cancellationToken: testContext.CancellationToken);
        body.GetProperty("subject").GetString().ShouldBe("Updated subject");
    }

    // Scenario: DELETE team-scoped template
    // WHEN an organizer deletes a team-scoped template
    // THEN the response is 204 No Content and a subsequent GET returns 404
    [TestMethod]
    public async Task SC006_DeleteTeamTemplate_ReturnsNoContent()
    {
        var fixture = AdminEmailTemplatesFixture.WithTeamTemplate();
        var version = await fixture.SetupTeamTemplateAsync(Environment);

        var response = await Environment.ApiClient.DeleteAsync(
            $"{AdminEmailTemplatesFixture.TeamTemplateRoute}?version={version}",
            testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var getResponse = await Environment.ApiClient.GetAsync(
            AdminEmailTemplatesFixture.TeamTemplateRoute,
            testContext.CancellationToken);
        getResponse.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    // Scenario: Delete event-scoped template falls back to team or default
    // WHEN an organizer deletes the event-scoped template while a team-scoped one still exists
    // THEN the response is 204 No Content and a subsequent GET on the event scope still returns 404
    [TestMethod]
    public async Task SC007_DeleteEventTemplate_ReturnsNoContent()
    {
        var fixture = AdminEmailTemplatesFixture.WithBothTemplates();
        var (_, eventVersion) = await fixture.SetupBothTemplatesAsync(Environment);

        var response = await Environment.ApiClient.DeleteAsync(
            $"{AdminEmailTemplatesFixture.EventTemplateRoute}?version={eventVersion}",
            testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        // The event-scoped template is gone; GET returns 404 (no fall-through at HTTP layer)
        var getResponse = await Environment.ApiClient.GetAsync(
            AdminEmailTemplatesFixture.EventTemplateRoute,
            testContext.CancellationToken);
        getResponse.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    // Scenario: Non-team-member denied
    // WHEN a user who is not a member of team "acme-templates" attempts to upsert a template
    // THEN the request is denied with a 403 Forbidden
    [TestMethod]
    public async Task SC008_NonMember_Denied_Returns403()
    {
        var fixture = AdminEmailTemplatesFixture.EmptyTemplates();
        await fixture.SetupEmptyAsync(Environment);

        var request = new
        {
            Subject = "Test",
            TextBody = "Test",
            HtmlBody = "<p>Test</p>",
            Version = (uint?)null
        };

        var response = await Environment.BobApiClient.PutAsJsonAsync(
            AdminEmailTemplatesFixture.TeamTemplateRoute,
            request,
            cancellationToken: testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    // Scenario: Reject update with stale version
    // WHEN an organizer submits an update with a Version older than the stored value
    // THEN the request is rejected with a 409 Conflict
    [TestMethod]
    public async Task SC009_UpdateWithStaleVersion_ReturnsConflict()
    {
        var fixture = AdminEmailTemplatesFixture.WithTeamTemplate();
        await fixture.SetupTeamTemplateAsync(Environment);

        var request = new
        {
            Subject = "Stale update",
            TextBody = "Stale text",
            HtmlBody = "<p>Stale html</p>",
            Version = (uint)9999
        };

        var response = await Environment.ApiClient.PutAsJsonAsync(
            AdminEmailTemplatesFixture.TeamTemplateRoute,
            request,
            cancellationToken: testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }
}
