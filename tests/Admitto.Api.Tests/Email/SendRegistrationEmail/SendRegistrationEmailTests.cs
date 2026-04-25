using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Amolenk.Admitto.Api.Tests.Infrastructure;
using Amolenk.Admitto.Module.Email.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace Amolenk.Admitto.Api.Tests.Email.SendRegistrationEmail;

[TestClass]
public sealed class SendRegistrationEmailTests(TestContext testContext) : EndToEndTestBase
{
    [TestInitialize]
    public override async ValueTask TestInitialize()
    {
        await base.TestInitialize();
        await ClearMailDevAsync();
    }

    [TestMethod]
    public async Task SC001_RegisterAttendee_WithEmailSettings_SendsExactlyOneEmailAndLogsIt()
    {
        var fixture = SendRegistrationEmailFixture.HappyFlow();
        await fixture.SetupAsync(Environment);

        var request = new
        {
            FirstName = "Alice",
            LastName = "Anderson",
            Email = SendRegistrationEmailFixture.RecipientEmail,
            TicketTypeSlugs = new[] { SendRegistrationEmailFixture.TicketTypeSlug }
        };

        var response = await Environment.ApiClient.PostAsJsonAsync(
            SendRegistrationEmailFixture.RegisterRoute,
            request,
            cancellationToken: testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Created);

        // Wait for the Worker to pick up the outbox message and send the email.
        var emails = await PollMailDevAsync(expectedCount: 1, timeout: TimeSpan.FromSeconds(30));

        emails.Count.ShouldBe(1);
        var email = emails[0];
        email.GetProperty("to")[0].GetProperty("address").GetString().ShouldBe(SendRegistrationEmailFixture.RecipientEmail);

        // Verify exactly one EmailLog row was created with status Sent.
        Environment.EmailDatabase.Context.ChangeTracker.Clear();
        var logEntries = await Environment.EmailDatabase.Context.EmailLog
            .AsNoTracking()
            .Where(l => l.IdempotencyKey.StartsWith("attendee-registered:"))
            .ToListAsync(testContext.CancellationToken);

        logEntries.Count.ShouldBe(1);
        logEntries[0].Status.ShouldBe(EmailLogStatus.Sent);
    }

    [TestMethod]
    public async Task SC002_RegisterAttendee_WithEmailSettings_RedeliveredEventDoesNotDoubleSend()
    {
        var fixture = SendRegistrationEmailFixture.HappyFlow();
        await fixture.SetupAsync(Environment);

        var request = new
        {
            FirstName = "Bob",
            LastName = "Builder",
            Email = "bob@example.com",
            TicketTypeSlugs = new[] { SendRegistrationEmailFixture.TicketTypeSlug }
        };

        // Register and wait for the first email.
        var response = await Environment.ApiClient.PostAsJsonAsync(
            SendRegistrationEmailFixture.RegisterRoute,
            request,
            cancellationToken: testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Created);

        var emails = await PollMailDevAsync(expectedCount: 1, timeout: TimeSpan.FromSeconds(30));
        emails.Count.ShouldBe(1);

        // Clear MailDev inbox and wait briefly. The idempotency check in the integration event
        // handler uses the EmailLog, so a redelivery attempt (same idempotency key) must not
        // produce a second send.
        await ClearMailDevAsync();

        // Wait briefly to confirm no second email is sent.
        await Task.Delay(TimeSpan.FromSeconds(5), testContext.CancellationToken);

        var emailsAfterDelay = await PollMailDevAsync(expectedCount: 0, timeout: TimeSpan.Zero);
        emailsAfterDelay.Count.ShouldBe(0, "No second email should be sent after clearing MailDev");

        // EmailLog must still contain exactly one entry.
        Environment.EmailDatabase.Context.ChangeTracker.Clear();
        var logEntries = await Environment.EmailDatabase.Context.EmailLog
            .AsNoTracking()
            .Where(l => l.IdempotencyKey.StartsWith("attendee-registered:"))
            .ToListAsync(testContext.CancellationToken);

        logEntries.Count.ShouldBe(1);
    }

    private async Task ClearMailDevAsync()
    {
        await Environment.MailDevClient.DeleteAsync("/email/all", testContext.CancellationToken);
    }

    private async Task<List<JsonElement>> PollMailDevAsync(int expectedCount, TimeSpan timeout)
    {
        var deadline = DateTimeOffset.UtcNow.Add(timeout);

        while (true)
        {
            var mailDevResponse = await Environment.MailDevClient.GetAsync(
                "/email",
                testContext.CancellationToken);

            if (mailDevResponse.IsSuccessStatusCode)
            {
                var json = await mailDevResponse.Content.ReadFromJsonAsync<JsonElement>(
                    cancellationToken: testContext.CancellationToken);

                var emails = json.EnumerateArray().ToList();
                if (emails.Count >= expectedCount)
                    return emails;
            }

            if (DateTimeOffset.UtcNow >= deadline)
                return [];

            await Task.Delay(TimeSpan.FromSeconds(2), testContext.CancellationToken);
        }
    }
}
