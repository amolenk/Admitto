using System.Net;
using System.Text.Json;
using Amolenk.Admitto.Api.Tests.Infrastructure;
using Shouldly;

namespace Amolenk.Admitto.Api.Tests.Email.BulkEmail;

[TestClass]
public sealed class RemovedBulkEmailRoutesTests(TestContext testContext) : EndToEndTestBase
{
    // Given the published OpenAPI route table
    // When a former bulk-email route is looked up
    // Then the route is absent because it is no longer registered
    [TestMethod]
    [DataRow("/admin/teams/{teamId}/events/{eventId}/bulk-emails/preview", "post")]
    [DataRow("/admin/teams/{teamId}/events/{eventId}/bulk-emails", "post")]
    [DataRow("/admin/teams/{teamId}/events/{eventId}/bulk-emails", "get")]
    [DataRow("/admin/teams/{teamId}/events/{eventId}/bulk-emails/{jobId}", "get")]
    [DataRow("/admin/teams/{teamId}/events/{eventId}/bulk-emails/{jobId}/cancel", "post")]
    public async Task HistoricalBulkEmailRoute_NotRegistered_IsAbsentFromOpenApi(
        string path,
        string method)
    {
        // Arrange
        var response = await Environment.AnonymousApiClient.GetAsync(
            "/openapi/v1.json",
            testContext.CancellationToken);
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var json = await response.Content.ReadAsStringAsync(testContext.CancellationToken);
        using var document = JsonDocument.Parse(json);

        // Act
        var paths = document.RootElement.GetProperty("paths");

        // Assert
        if (paths.TryGetProperty(path, out var route))
        {
            route.TryGetProperty(method, out _).ShouldBeFalse();
        }
    }
}
