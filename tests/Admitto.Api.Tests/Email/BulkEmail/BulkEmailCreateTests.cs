using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Amolenk.Admitto.Api.Tests.Infrastructure;
using Amolenk.Admitto.Module.Email.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace Amolenk.Admitto.Api.Tests.Email.BulkEmail;

[TestClass]
public sealed class BulkEmailCreateTests(TestContext testContext) : EndToEndTestBase
{
    [TestInitialize]
    public override async ValueTask TestInitialize()
    {
        await base.TestInitialize();
        await Environment.ClearAsync(testContext.CancellationToken);
    }

    // SC-8.2: saved-template path creates a job that fans out, hits MailDev, and produces
    // EmailLog rows tagged with the bulk-job id.
    [TestMethod]
    public async Task SC001_CreateWithSavedTemplate_FansOutAndLogsEachRecipient()
    {
        var fixture = BulkEmailFixture.Empty()
            .WithTicketTemplate()
            .WithRegistration("alice@example.com", "Alice", "Anderson")
            .WithRegistration("bob@example.com", "Bob", "Brown");
        await fixture.SetupAsync(Environment);

        var request = new
        {
            EmailType = BulkEmailFixture.EmailType,
            Source = new { Attendee = new { } }
        };

        var createResponse = await Environment.ApiClient.PostAsJsonAsync(
            BulkEmailFixture.CreateRoute,
            request,
            cancellationToken: testContext.CancellationToken);

        createResponse.StatusCode.ShouldBe(HttpStatusCode.Created);
        var createBody = await createResponse.Content.ReadFromJsonAsync<JsonElement>(
            cancellationToken: testContext.CancellationToken);
        var bulkJobId = createBody.GetProperty("bulkEmailJobId").GetGuid();

        var emails = await Environment.PollAsync(
            expectedCount: 2,
            timeout: TimeSpan.FromSeconds(45),
            ct: testContext.CancellationToken);

        emails.Count.ShouldBe(2);
        emails.RecipientAddresses().ShouldBe(
            new[] { "alice@example.com", "bob@example.com" }, ignoreOrder: true);

        // Every EmailLog row written by the fan-out must carry the originating bulk-job id.
        await WaitForEmailLogsAsync(bulkJobId, expectedCount: 2);

        var logs = await Environment.EmailDatabase.Context.EmailLog
            .AsNoTracking()
            .Where(l => l.BulkEmailJobId == BulkEmailJobId.From(bulkJobId))
            .ToListAsync(testContext.CancellationToken);

        logs.Count.ShouldBe(2);
        logs.ShouldAllBe(l => l.Status == EmailLogStatus.Sent);
        logs.ShouldAllBe(l => l.IdempotencyKey.StartsWith($"bulk:{bulkJobId:N}:"));
    }

    // SC-8.2: ad-hoc path (no template, subject + bodies on the request) also fans out and logs.
    [TestMethod]
    public async Task SC002_CreateWithAdHocOverrides_FansOutAndLogsEachRecipient()
    {
        var fixture = BulkEmailFixture.Empty()
            .WithTicketTemplate() // template still required, ad-hoc only overrides
            .WithRegistration("dana@example.com", "Dana", "Daniels");
        await fixture.SetupAsync(Environment);

        var request = new
        {
            EmailType = BulkEmailFixture.EmailType,
            Subject = "Custom subject for {{ first_name }}",
            TextBody = "Custom text for {{ first_name }}",
            HtmlBody = "<p>Custom html for {{ first_name }}</p>",
            Source = new { Attendee = new { } }
        };

        var createResponse = await Environment.ApiClient.PostAsJsonAsync(
            BulkEmailFixture.CreateRoute,
            request,
            cancellationToken: testContext.CancellationToken);

        createResponse.StatusCode.ShouldBe(HttpStatusCode.Created);
        var bulkJobId = (await createResponse.Content.ReadFromJsonAsync<JsonElement>(
            cancellationToken: testContext.CancellationToken))
            .GetProperty("bulkEmailJobId").GetGuid();

        var emails = await Environment.PollAsync(
            expectedCount: 1,
            timeout: TimeSpan.FromSeconds(45),
            ct: testContext.CancellationToken);

        emails.Count.ShouldBe(1);
        emails[0].GetProperty("subject").GetString().ShouldBe("Custom subject for Dana");

        await WaitForEmailLogsAsync(bulkJobId, expectedCount: 1);

        var log = await Environment.EmailDatabase.Context.EmailLog
            .AsNoTracking()
            .SingleAsync(
                l => l.BulkEmailJobId == BulkEmailJobId.From(bulkJobId),
                testContext.CancellationToken);

        log.Subject.ShouldBe("Custom subject for Dana");
        log.Status.ShouldBe(EmailLogStatus.Sent);
    }

    private async Task WaitForEmailLogsAsync(Guid bulkJobId, int expectedCount)
    {
        var deadline = DateTimeOffset.UtcNow.AddSeconds(15);
        while (DateTimeOffset.UtcNow < deadline)
        {
            Environment.EmailDatabase.Context.ChangeTracker.Clear();
            var count = await Environment.EmailDatabase.Context.EmailLog
                .AsNoTracking()
                .CountAsync(l => l.BulkEmailJobId == BulkEmailJobId.From(bulkJobId), testContext.CancellationToken);
            if (count >= expectedCount) return;
            await Task.Delay(TimeSpan.FromMilliseconds(500), testContext.CancellationToken);
        }
    }
}
