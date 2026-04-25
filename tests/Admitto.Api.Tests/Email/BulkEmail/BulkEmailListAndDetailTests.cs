using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Amolenk.Admitto.Api.Tests.Infrastructure;
using Shouldly;

namespace Amolenk.Admitto.Api.Tests.Email.BulkEmail;

[TestClass]
public sealed class BulkEmailListAndDetailTests(TestContext testContext) : EndToEndTestBase
{
    [TestInitialize]
    public override async ValueTask TestInitialize()
    {
        await base.TestInitialize();
        await Environment.ClearAsync(testContext.CancellationToken);
    }

    // SC-8.4: GET / returns the job in the list, GET /{id} returns full detail with
    // per-recipient status visible after fan-out completes.
    [TestMethod]
    public async Task SC001_ListAndDetail_ReturnPerRecipientStatus()
    {
        var fixture = BulkEmailFixture.Empty()
            .WithTicketTemplate()
            .WithRegistration("ann@example.com", "Ann", "A")
            .WithRegistration("ben@example.com", "Ben", "B");
        await fixture.SetupAsync(Environment);

        var createResponse = await Environment.ApiClient.PostAsJsonAsync(
            BulkEmailFixture.CreateRoute,
            new
            {
                EmailType = BulkEmailFixture.EmailType,
                Source = new { Attendee = new { } }
            },
            cancellationToken: testContext.CancellationToken);
        createResponse.StatusCode.ShouldBe(HttpStatusCode.Created);
        var bulkJobId = (await createResponse.Content.ReadFromJsonAsync<JsonElement>(
            cancellationToken: testContext.CancellationToken))
            .GetProperty("bulkEmailJobId").GetGuid();

        // Wait for fan-out to complete by watching MailDev.
        await Environment.PollAsync(2, TimeSpan.FromSeconds(45), testContext.CancellationToken);

        var detail = await PollUntilTerminalAsync(bulkJobId);
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
            BulkEmailFixture.ListRoute, testContext.CancellationToken);
        listResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

        var listBody = await listResponse.Content.ReadFromJsonAsync<JsonElement>(
            cancellationToken: testContext.CancellationToken);
        var ids = listBody.EnumerateArray()
            .Select(j => j.GetProperty("id").GetGuid())
            .ToList();
        ids.ShouldContain(bulkJobId);
    }

    private async Task<JsonElement> PollUntilTerminalAsync(Guid bulkJobId)
    {
        var deadline = DateTimeOffset.UtcNow.AddSeconds(60);
        JsonElement last = default;
        while (DateTimeOffset.UtcNow < deadline)
        {
            var response = await Environment.ApiClient.GetAsync(
                BulkEmailFixture.DetailRoute(bulkJobId),
                testContext.CancellationToken);
            if (response.IsSuccessStatusCode)
            {
                last = await response.Content.ReadFromJsonAsync<JsonElement>(
                    cancellationToken: testContext.CancellationToken);
                var status = last.GetProperty("status").GetString();
                if (status is "completed" or "partiallyFailed" or "failed" or "cancelled")
                    return last;
            }
            await Task.Delay(TimeSpan.FromMilliseconds(500), testContext.CancellationToken);
        }
        throw new TimeoutException(
            $"Bulk-email job {bulkJobId} never reached a terminal state. Last observed: {last}");
    }
}
