using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Amolenk.Admitto.Api.Tests.Infrastructure;
using Shouldly;

namespace Amolenk.Admitto.Api.Tests.Email.BulkEmail;

[TestClass]
public sealed class BulkEmailListAndDetailTests(TestContext testContext) : EndToEndTestBase
{
    // SC-8.4: GET / returns the job in the list, GET /{id} returns full detail with
    // per-recipient status visible after fan-out completes.
    [TestMethod]
    public async Task ListAndDetail_ReturnPerRecipientStatus()
    {
        var fixture = BulkEmailFixture.Empty()
            .WithRegistration("ann@example.com", "Ann", "A")
            .WithRegistration("ben@example.com", "Ben", "B");
        await fixture.SetupAsync(Environment);

        var createResponse = await Environment.ApiClient.PostAsJsonAsync(
            fixture.CreateRoute,
            new
            {
                EmailType = "bulk-custom",
                Subject = "Hello {{ first_name }}",
                TextBody = "Hi {{ first_name }}",
                HtmlBody = "<p>Hi {{ first_name }}</p>",
                Source = new { Attendee = new { } }
            },
            cancellationToken: testContext.CancellationToken);
        createResponse.StatusCode.ShouldBe(HttpStatusCode.Created);
        var bulkJobId = (await createResponse.Content.ReadFromJsonAsync<JsonElement>(
            cancellationToken: testContext.CancellationToken))
            .GetProperty("bulkEmailJobId").GetGuid();

        // Wait for fan-out to complete by watching status.
        // await Environment.PollEmailAsync(2, TimeSpan.FromSeconds(90), testContext.CancellationToken);

        var detail = await fixture.PollUntilTerminalAsync(bulkJobId, Environment, testContext.CancellationToken);
        detail.GetProperty("status").GetString().ShouldBe("completed");
        detail.GetProperty("recipientCount").GetInt32().ShouldBe(2);
        detail.GetProperty("sentCount").GetInt32().ShouldBe(2);

        var recipients = detail.GetProperty("recipients").EnumerateArray().ToList();
        recipients.Count.ShouldBe(2);
        recipients.Select(r => r.GetProperty("email").GetString())
            .ShouldBe(new[] { "ann@example.com", "ben@example.com" }, ignoreOrder: true);
        recipients.ShouldAllBe(r => r.GetProperty("status").GetString() == "sent");

        // List endpoint must include the job we just created.
        var listResponse = await Environment.ApiClient.GetAsync(
            fixture.ListRoute, testContext.CancellationToken);
        listResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

        var listBody = await listResponse.Content.ReadFromJsonAsync<JsonElement>(
            cancellationToken: testContext.CancellationToken);
        var ids = listBody.EnumerateArray()
            .Select(j => j.GetProperty("id").GetGuid())
            .ToList();
        ids.ShouldContain(bulkJobId);
    }
}
