using System.Net;
using System.Net.Http.Json;
using Amolenk.Admitto.Api.Tests.Infrastructure;
using Shouldly;

namespace Amolenk.Admitto.Api.Tests.Organization.TicketedEventManagement;

[TestClass]
public sealed class CreateTicketedEventTests(TestContext testContext) : EndToEndTestBase
{
    private static readonly object ValidRequest = new
    {
        Slug = "my-conference",
        Name = "My Conference",
        WebsiteUrl = "https://example.com",
        BaseUrl = "https://example.com/events",
        StartsAt = new DateTimeOffset(2026, 9, 1, 9, 0, 0, TimeSpan.Zero),
        EndsAt = new DateTimeOffset(2026, 9, 2, 17, 0, 0, TimeSpan.Zero),
        TimeZone = "Europe/Amsterdam"
    };

    // SC-001: Given a valid event-creation request for team "test-team",
    //         when Alice (team owner) submits the request,
    //         then the creation request transitions to status "Created"
    //         and the response contains the new event's ID.
    [TestMethod]
    public async Task SC001_ValidRequest_EventReachesCreatedStatus()
    {
        // Arrange
        var fixture = CreateTicketedEventFixture.WithTeam();
        await fixture.SetupAsync(Environment);

        // Act - submit creation request
        var postResponse = await Environment.ApiClient.PostAsJsonAsync(
            CreateTicketedEventFixture.EventCreationsRoute,
            ValidRequest,
            cancellationToken: testContext.CancellationToken);

        postResponse.StatusCode.ShouldBe(HttpStatusCode.Accepted);

        var location = postResponse.Headers.Location?.ToString();
        location.ShouldNotBeNullOrEmpty();

        var creationRequestId = location!.Split('/').Last();

        // Poll until the creation request is settled (max ~30 s)
        string? finalStatus = null;
        string? responseBody = null;
        for (var attempt = 0; attempt < 60; attempt++)
        {
            await Task.Delay(500, testContext.CancellationToken);

            var getResponse = await Environment.ApiClient.GetAsync(
                CreateTicketedEventFixture.EventCreationStatusRoute(creationRequestId),
                testContext.CancellationToken);

            getResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

            responseBody = await getResponse.Content.ReadAsStringAsync(testContext.CancellationToken);
            using var doc = System.Text.Json.JsonDocument.Parse(responseBody);

            finalStatus = doc.RootElement.GetProperty("status").GetString();
            if (finalStatus is "Created" or "Rejected")
                break;
        }

        // Assert
        finalStatus.ShouldBe("Created", $"Expected status 'Created' but was '{finalStatus}'. Response: {responseBody}");
    }
}
