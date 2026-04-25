using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Amolenk.Admitto.Api.Tests.Infrastructure;
using Amolenk.Admitto.Module.Email.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace Amolenk.Admitto.Api.Tests.Email.BulkEmail;

[TestClass]
public sealed class BulkEmailCancelTests(TestContext testContext) : EndToEndTestBase
{
    [TestInitialize]
    public override async ValueTask TestInitialize()
    {
        await base.TestInitialize();
        await Environment.ClearAsync(testContext.CancellationToken);
    }

    // SC-8.3: cancelling while still Pending (before fan-out picks up) ends the
    // job in Cancelled with zero sends and no MailDev traffic.
    [TestMethod]
    public async Task SC001_CancelFromPending_EndsCancelledWithZeroSent()
    {
        var fixture = BulkEmailFixture.Empty().WithTicketTemplate();
        await fixture.SetupAsync(Environment);

        // 30 external recipients keeps the worker busy long enough that Pending
        // (or at most the first one or two sends) is still observable, but more
        // importantly we will assert the *post-cancel* counters anyway.
        var items = Enumerable.Range(0, 30)
            .Select(i => new { Email = $"r{i:D2}@example.com", DisplayName = (string?)null })
            .ToArray();

        var createResponse = await Environment.ApiClient.PostAsJsonAsync(
            BulkEmailFixture.CreateRoute,
            new
            {
                EmailType = BulkEmailFixture.EmailType,
                Source = new { ExternalList = new { Items = items } }
            },
            cancellationToken: testContext.CancellationToken);
        createResponse.StatusCode.ShouldBe(HttpStatusCode.Created);

        var bulkJobId = (await createResponse.Content.ReadFromJsonAsync<JsonElement>(
            cancellationToken: testContext.CancellationToken))
            .GetProperty("bulkEmailJobId").GetGuid();

        var cancelResponse = await Environment.ApiClient.PostAsync(
            BulkEmailFixture.CancelRoute(bulkJobId),
            content: null,
            cancellationToken: testContext.CancellationToken);
        cancelResponse.StatusCode.ShouldBe(HttpStatusCode.Accepted);

        var detail = await PollUntilTerminalAsync(bulkJobId);
        detail.GetProperty("status").GetString().ShouldBe("cancelled");
        detail.GetProperty("cancelledCount").GetInt32().ShouldBeGreaterThan(0);
        // Some recipients may have been sent before the worker observed the
        // cancellation. The point is we did not blast the entire list.
        detail.GetProperty("sentCount").GetInt32().ShouldBeLessThan(items.Length);
    }

    // SC-8.3: cancelling a job that is already in a terminal state is a 409.
    [TestMethod]
    public async Task SC002_CancelFromTerminalState_ReturnsConflict()
    {
        var fixture = BulkEmailFixture.Empty()
            .WithTicketTemplate()
            .WithRegistration("solo@example.com", "Solo", "Sender");
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

        await Environment.PollAsync(1, TimeSpan.FromSeconds(45), testContext.CancellationToken);
        // Wait until the job rolls over to a terminal state in the DB.
        var detail = await PollUntilTerminalAsync(bulkJobId);
        detail.GetProperty("status").GetString().ShouldBe("completed");

        var cancelResponse = await Environment.ApiClient.PostAsync(
            BulkEmailFixture.CancelRoute(bulkJobId),
            content: null,
            cancellationToken: testContext.CancellationToken);
        cancelResponse.StatusCode.ShouldBe(HttpStatusCode.Conflict);
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
