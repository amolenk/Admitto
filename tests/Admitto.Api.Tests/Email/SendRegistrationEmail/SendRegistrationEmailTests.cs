using System.Net;
using System.Net.Http.Json;
using Amolenk.Admitto.Api.Tests.Infrastructure;
using Amolenk.Admitto.Core.Email.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace Amolenk.Admitto.Api.Tests.Email.SendRegistrationEmail;

[TestClass]
public sealed class SendRegistrationEmailTests(TestContext testContext) : EndToEndTestBase
{
    // Given an event configured with system email settings
    // When an attendee registers
    // Then exactly one confirmation email is sent and exactly one EmailLog row with status Sent is created
    [TestMethod]
    public async Task RegisterAttendee_WithSystemEmailSettings_SendsExactlyOneEmailAndLogsIt()
    {
        var fixture = SendRegistrationEmailFixture.HappyFlow();
        await fixture.SetupAsync(Environment);

        var request = new
        {
            FirstName = "Alice",
            LastName = "Anderson",
            Email = SendRegistrationEmailFixture.RecipientEmail,
            TicketTypeIds = new[] { SendRegistrationEmailFixture.TicketTypeId.Value }
        };

        var response = await Environment.ApiClient.PostAsJsonAsync(
            fixture.RegisterRoute,
            request,
            cancellationToken: testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Created);

        // Wait for the Worker to pick up the outbox message and send the email.
        var emails = await Environment.Email.WaitForAsync(
            expectedCount: 1,
            timeout: TimeSpan.FromSeconds(30),
            testContext.CancellationToken);

        emails.Count.ShouldBe(1);
        var email = emails[0];
        email.GetProperty("to")[0].GetProperty("address").GetString()
            .ShouldBe(SendRegistrationEmailFixture.RecipientEmail);

        // Verify exactly one EmailLog row was created with status Sent.
        Environment.EmailDatabase.Context.ChangeTracker.Clear();
        var logEntries = await Environment.EmailDatabase.Context.EmailLog
            .AsNoTracking()
            .Where(l => l.IdempotencyKey.StartsWith("attendee-registered:"))
            .ToListAsync(testContext.CancellationToken);

        logEntries.Count.ShouldBe(1);
        logEntries[0].Status.ShouldBe(EmailLogStatus.Sent);
    }

    // Given an attendee who has already registered and received a confirmation email
    // When the registration event is redelivered to the integration event handler
    // Then no second email is sent and only one EmailLog row remains
    [TestMethod]
    public async Task RegisterAttendee_WithSystemEmailSettings_RedeliveredEventDoesNotDoubleSend()
    {
        var fixture = SendRegistrationEmailFixture.HappyFlow();
        await fixture.SetupAsync(Environment);

        var request = new
        {
            FirstName = "Bob",
            LastName = "Builder",
            Email = "bob@example.com",
            TicketTypeIds = new[] { SendRegistrationEmailFixture.TicketTypeId.Value }
        };

        // Register and wait for the first email.
        var response = await Environment.ApiClient.PostAsJsonAsync(
            fixture.RegisterRoute,
            request,
            cancellationToken: testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Created);

        var emails = await Environment.Email.WaitForAsync(
            expectedCount: 1,
            timeout: TimeSpan.FromSeconds(30),
            testContext.CancellationToken);
        emails.Count.ShouldBe(1);

        // Clear MailDev inbox. The idempotency check in the integration event
        // handler uses the EmailLog, so a redelivery attempt (same idempotency key) must not
        // produce a second send.
        await Environment.Email.ResetAsync();

        // Wait briefly to confirm no second email is sent.
        await Task.Delay(TimeSpan.FromSeconds(5), testContext.CancellationToken);

        var emailsAfterDelay = await Environment.Email.WaitForAsync(
            expectedCount: 0,
            timeout: TimeSpan.Zero,
            testContext.CancellationToken);
        emailsAfterDelay.Count.ShouldBe(0, "No second email should be sent after clearing MailDev");

        // EmailLog must still contain exactly one entry.
        Environment.EmailDatabase.Context.ChangeTracker.Clear();
        var logEntries = await Environment.EmailDatabase.Context.EmailLog
            .AsNoTracking()
            .Where(l => l.IdempotencyKey.StartsWith("attendee-registered:"))
            .ToListAsync(testContext.CancellationToken);

        logEntries.Count.ShouldBe(1);
    }
}
