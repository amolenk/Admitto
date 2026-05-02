using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Amolenk.Admitto.Api.Tests.Infrastructure;
using Shouldly;

namespace Amolenk.Admitto.Api.Tests.Registrations.GetAttendeeEmails;

[TestClass]
public sealed class GetAttendeeEmailsTests(TestContext testContext) : EndToEndTestBase
{
    [TestMethod]
    public async Task SC001_Organizer_ReturnsEmailList()
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

    [TestMethod]
    public async Task SC002_NoEmails_ReturnsEmptyList()
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

    [TestMethod]
    public async Task SC003_NonMember_Returns403()
    {
        var fixture = GetAttendeeEmailsFixture.Empty();
        await fixture.SetupAsync(Environment);

        var response = await Environment.BobApiClient.GetAsync(
            fixture.Route, testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }
}
