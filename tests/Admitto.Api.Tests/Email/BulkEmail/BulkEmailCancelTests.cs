using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Amolenk.Admitto.Api.Tests.Infrastructure;
using Shouldly;

namespace Amolenk.Admitto.Api.Tests.Email.BulkEmail;

[TestClass]
public sealed class BulkEmailCancelTests(TestContext testContext) : EndToEndTestBase
{
    // Cancelling a job that is already in a terminal state is a 409.
    [TestMethod]
    public async Task CancelFromTerminalState_ReturnsConflict()
    {
        var fixture = BulkEmailFixture.Empty()
            .WithRegistration("solo@example.com", "Solo", "Sender");
        await fixture.SetupAsync(Environment);

        var createResponse = await Environment.ApiClient.PostAsJsonAsync(
            fixture.CreateRoute,
            new
            {
                EmailType = "bulk-custom",
                Subject = "Hello {{ first_name }}",
                TextBody = "Hi {{ first_name }}",
                HtmlBody = "<p>Hi {{ first_name }}</p>",
                AttendeeFilter = new { }
            },
            cancellationToken: testContext.CancellationToken);
        createResponse.StatusCode.ShouldBe(HttpStatusCode.Created);
        var bulkJobId = (await createResponse.Content.ReadFromJsonAsync<JsonElement>(
                cancellationToken: testContext.CancellationToken))
            .GetProperty("bulkEmailJobId").GetGuid();

        await Environment.Email.WaitForAsync(1, TimeSpan.FromSeconds(90), testContext.CancellationToken);

        // Wait until the job rolls over to a terminal state in the DB.
        var detail = await fixture.PollUntilTerminalAsync(bulkJobId, Environment, testContext.CancellationToken);
        detail.GetProperty("status").GetString().ShouldBe("completed");

        var cancelResponse = await Environment.ApiClient.PostAsync(
            fixture.CancelRoute(bulkJobId),
            content: null,
            cancellationToken: testContext.CancellationToken);
        cancelResponse.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }
}
