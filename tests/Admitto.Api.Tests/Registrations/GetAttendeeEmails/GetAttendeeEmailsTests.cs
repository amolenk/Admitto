using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Amolenk.Admitto.Api.Tests.Infrastructure;
using Shouldly;

namespace Amolenk.Admitto.Api.Tests.Registrations.GetAttendeeEmails;

[TestClass]
public sealed class GetAttendeeEmailsTests(TestContext testContext) : EndToEndTestBase
{
    // Given a registration with a sent confirmation email logged
    // When a team member fetches the attendee's email history
    // Then the API returns 200 OK with the confirmation email's details
    [TestMethod]
    public async Task Organizer_ReturnsEmailList()
    {
        var fixture = GetAttendeeEmailsFixture.WithEmails();
        await fixture.SetupAsync(Environment);

        var response = await Environment.ApiClient.GetAsync(
            fixture.Route, testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<JsonElement[]>(
            cancellationToken: testContext.CancellationToken);
        body.ShouldNotBeNull();
        body.ShouldHaveSingleItem();
        body[0].GetProperty("subject").GetString().ShouldBe("Your DevConf registration");
        body[0].GetProperty("emailType").GetString().ShouldBe("Confirmation");
        body[0].GetProperty("status").GetString().ShouldBe("Sent");
    }

    // Given a registration with no emails sent
    // When a team member fetches the attendee's email history
    // Then the API returns 200 OK with an empty list
    [TestMethod]
    public async Task NoEmails_ReturnsEmptyList()
    {
        var fixture = GetAttendeeEmailsFixture.Empty();
        await fixture.SetupAsync(Environment);

        var response = await Environment.ApiClient.GetAsync(
            fixture.Route, testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<JsonElement[]>(
            cancellationToken: testContext.CancellationToken);
        body.ShouldNotBeNull();
        body.ShouldBeEmpty();
    }

    // Given a registration with no emails sent
    // When a user who is not a member of the team fetches the attendee's email history
    // Then the API returns 403 Forbidden
    [TestMethod]
    public async Task NonMember_Returns403()
    {
        var fixture = GetAttendeeEmailsFixture.Empty();
        await fixture.SetupAsync(Environment);

        var response = await Environment.BobApiClient.GetAsync(
            fixture.Route, testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }
}
