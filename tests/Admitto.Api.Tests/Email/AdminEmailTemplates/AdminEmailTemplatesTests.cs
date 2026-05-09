using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Amolenk.Admitto.Api.Tests.Infrastructure;
using Amolenk.Admitto.Core.Email.Application.Templating;
using Shouldly;

namespace Amolenk.Admitto.Api.Tests.Email.AdminEmailTemplates;

[TestClass]
public sealed class AdminEmailTemplatesTests(TestContext testContext) : EndToEndTestBase
{
    // SC001: Create team-scoped built-in template
    // WHEN an organizer POSTs a new built-in template for team "acme-templates"
    // THEN the response is 201 Created and returns a GUID id
    [TestMethod]
    public async Task SC001_CreateTeamTemplate_ReturnsCreated()
    {
        var fixture = AdminEmailTemplatesFixture.EmptyTemplates();
        await fixture.SetupEmptyAsync(Environment);

        var request = new
        {
            Name = AdminEmailTemplatesFixture.TemplateName,
            Subject = "Welcome to {{ event_name }}",
            TextBody = "Hello {{ first_name }}",
            HtmlBody = "<p>Hello {{ first_name }}</p>"
        };

        var response = await Environment.ApiClient.PostAsJsonAsync(
            fixture.TeamTemplatesRoute,
            request,
            cancellationToken: testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Created);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>(
            cancellationToken: testContext.CancellationToken);
        body.GetProperty("id").GetGuid().ShouldNotBe(Guid.Empty);
    }

    // SC002: Create event-scoped built-in template
    // WHEN an organizer POSTs a new built-in template for event "templatesconf"
    // THEN the response is 201 Created
    [TestMethod]
    public async Task SC002_CreateEventTemplate_ReturnsCreated()
    {
        var fixture = AdminEmailTemplatesFixture.EmptyTemplates();
        await fixture.SetupEmptyAsync(Environment);

        var request = new
        {
            Name = AdminEmailTemplatesFixture.TemplateName,
            Subject = "Event ticket: {{ event_name }}",
            TextBody = "Hi {{ first_name }}, here is your ticket.",
            HtmlBody = "<p>Hi {{ first_name }}, here is your ticket.</p>"
        };

        var response = await Environment.ApiClient.PostAsJsonAsync(
            fixture.EventTemplatesRoute,
            request,
            cancellationToken: testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
    }

    // SC003: GET team-scoped template
    // WHEN an organizer reads a team-scoped ticket template
    // THEN the response is 200 OK with the stored subject and bodies
    [TestMethod]
    public async Task SC003_GetTeamTemplate_ReturnsOk()
    {
        var fixture = AdminEmailTemplatesFixture.WithTeamTemplate();
        var (id, _) = await fixture.SetupTeamTemplateAsync(Environment);

        var response = await Environment.ApiClient.GetAsync(
            fixture.TeamTemplateRoute(id),
            testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>(
            cancellationToken: testContext.CancellationToken);
        body.GetProperty("subject").GetString().ShouldBe("Team subject");
        body.GetProperty("textBody").GetString().ShouldNotBeNullOrEmpty();
        body.GetProperty("htmlBody").GetString().ShouldNotBeNullOrEmpty();
    }

    // SC004: GET event-scoped template
    // WHEN an organizer reads an event-scoped ticket template
    // THEN the response is 200 OK with the event-scoped subject
    [TestMethod]
    public async Task SC004_GetEventTemplate_ReturnsOk()
    {
        var fixture = AdminEmailTemplatesFixture.WithBothTemplates();
        var (_, _, eventTemplateId, _) = await fixture.SetupBothTemplatesAsync(Environment);

        var response = await Environment.ApiClient.GetAsync(
            fixture.EventTemplateRoute(eventTemplateId),
            testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>(
            cancellationToken: testContext.CancellationToken);
        body.GetProperty("subject").GetString().ShouldBe("Event subject");
    }

    // SC005: Update team-scoped template with correct version
    // WHEN an organizer submits an update with the correct version
    // THEN the response is 200 OK and a subsequent GET returns the updated content
    [TestMethod]
    public async Task SC005_UpdateTeamTemplate_WithCorrectVersion_ReturnsOk()
    {
        var fixture = AdminEmailTemplatesFixture.WithTeamTemplate();
        var (id, version) = await fixture.SetupTeamTemplateAsync(Environment);

        var request = new
        {
            Subject = "Updated subject",
            TextBody = "Updated text",
            HtmlBody = "<p>Updated html</p>",
            Version = version
        };

        var response = await Environment.ApiClient.PutAsJsonAsync(
            fixture.TeamTemplateRoute(id),
            request,
            cancellationToken: testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var getResponse = await Environment.ApiClient.GetAsync(
            fixture.TeamTemplateRoute(id),
            testContext.CancellationToken);
        var body = await getResponse.Content.ReadFromJsonAsync<JsonElement>(
            cancellationToken: testContext.CancellationToken);
        body.GetProperty("subject").GetString().ShouldBe("Updated subject");
    }

    // SC006: DELETE team-scoped template
    // WHEN an organizer deletes a team-scoped template
    // THEN the response is 204 No Content and the list shows the built-in as not customised
    [TestMethod]
    public async Task SC006_DeleteTeamTemplate_ReturnsNoContent()
    {
        var fixture = AdminEmailTemplatesFixture.WithTeamTemplate();
        var (id, version) = await fixture.SetupTeamTemplateAsync(Environment);

        var response = await Environment.ApiClient.DeleteAsync(
            $"{fixture.TeamTemplateRoute(id)}?version={version}",
            testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        // After deletion the list should show the built-in as virtual (isCustomised: false)
        var listResponse = await Environment.ApiClient.GetAsync(
            fixture.TeamTemplatesRoute,
            testContext.CancellationToken);
        listResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

        var list = await listResponse.Content.ReadFromJsonAsync<JsonElement>(
            cancellationToken: testContext.CancellationToken);
        var ticketEntry = list.EnumerateArray()
            .FirstOrDefault(e => e.GetProperty("name").GetString() == BuiltInEmailTemplateNames.TicketConfirmation);
        ticketEntry.ValueKind.ShouldNotBe(JsonValueKind.Undefined);
        ticketEntry.GetProperty("isCustomised").GetBoolean().ShouldBeFalse();
    }

    // SC007: Delete event-scoped template
    // WHEN an organizer deletes the event-scoped template
    // THEN the response is 204 No Content
    [TestMethod]
    public async Task SC007_DeleteEventTemplate_ReturnsNoContent()
    {
        var fixture = AdminEmailTemplatesFixture.WithBothTemplates();
        var (_, _, eventTemplateId, eventVersion) = await fixture.SetupBothTemplatesAsync(Environment);

        var response = await Environment.ApiClient.DeleteAsync(
            $"{fixture.EventTemplateRoute(eventTemplateId)}?version={eventVersion}",
            testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }

    // SC008: Non-team-member denied
    // WHEN a user who is not a member of team "acme-templates" attempts to create a template
    // THEN the request is denied with a 403 Forbidden
    [TestMethod]
    public async Task SC008_NonMember_Denied_Returns403()
    {
        var fixture = AdminEmailTemplatesFixture.EmptyTemplates();
        await fixture.SetupEmptyAsync(Environment);

        var request = new
        {
            Name = AdminEmailTemplatesFixture.TemplateName,
            Subject = "Test",
            TextBody = "Test",
            HtmlBody = "<p>Test</p>"
        };

        var response = await Environment.BobApiClient.PostAsJsonAsync(
            fixture.TeamTemplatesRoute,
            request,
            cancellationToken: testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    // SC009: Reject update with stale version
    // WHEN an organizer submits an update with a Version older than the stored value
    // THEN the request is rejected with a 409 Conflict
    [TestMethod]
    public async Task SC009_UpdateWithStaleVersion_ReturnsConflict()
    {
        var fixture = AdminEmailTemplatesFixture.WithTeamTemplate();
        var (id, _) = await fixture.SetupTeamTemplateAsync(Environment);

        var request = new
        {
            Subject = "Stale update",
            TextBody = "Stale text",
            HtmlBody = "<p>Stale html</p>",
            Version = (uint)9999
        };

        var response = await Environment.ApiClient.PutAsJsonAsync(
            fixture.TeamTemplateRoute(id),
            request,
            cancellationToken: testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }

    // SC010: List templates includes virtual built-ins
    // WHEN an organizer lists templates for a team with no customisations
    // THEN the list includes all built-in templates with isCustomised: false and no id
    [TestMethod]
    public async Task SC010_ListTemplates_IncludesVirtualBuiltIns()
    {
        var fixture = AdminEmailTemplatesFixture.EmptyTemplates();
        await fixture.SetupEmptyAsync(Environment);

        var response = await Environment.ApiClient.GetAsync(
            fixture.TeamTemplatesRoute,
            testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var list = await response.Content.ReadFromJsonAsync<JsonElement>(
            cancellationToken: testContext.CancellationToken);
        var items = list.EnumerateArray().ToList();
        items.Count.ShouldBeGreaterThan(0);

        var ticket = items.FirstOrDefault(e => e.GetProperty("name").GetString() == BuiltInEmailTemplateNames.TicketConfirmation);
        ticket.ValueKind.ShouldNotBe(JsonValueKind.Undefined);
        ticket.GetProperty("kind").GetString().ShouldBe("builtin");
        ticket.GetProperty("isCustomised").GetBoolean().ShouldBeFalse();
    }
}
