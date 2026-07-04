using System.Net;
using System.Net.Http.Json;
using Amolenk.Admitto.Api.Tests.Infrastructure;
using Amolenk.Admitto.Testing.Builders.Organization.Application;
using Shouldly;

namespace Amolenk.Admitto.Api.Tests.Email.RemovedEmailSettings;

[TestClass]
public sealed class RemovedEmailSettingsRoutesTests(TestContext testContext) : EndToEndTestBase
{
    [TestMethod]
    public async Task GetTeamEmailSettings_RemovedRoute_ReturnsNotFound()
    {
        var teamId = await SeedTeamAsync();

        var response = await Environment.ApiClient.GetAsync(
            $"/admin/teams/{teamId}/email-settings",
            testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [TestMethod]
    public async Task TestTeamEmailSettings_RemovedRoute_ReturnsNotFound()
    {
        var teamId = await SeedTeamAsync();

        var response = await Environment.ApiClient.PostAsJsonAsync(
            $"/admin/teams/{teamId}/email-settings/test",
            new { Recipient = "ops@example.com" },
            testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    private async ValueTask<Guid> SeedTeamAsync()
    {
        var team = new TeamBuilder().Build();
        await Environment.OrganizationDatabase.SeedAsync(db => db.Teams.Add(team));
        return team.Id.Value;
    }
}
