using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Amolenk.Admitto.Api.Tests.Infrastructure;
using Amolenk.Admitto.Testing.Infrastructure.TestContexts;
using Shouldly;

namespace Amolenk.Admitto.Api.Tests.Email.BulkEmail;

[TestClass]
public sealed class ReconfirmFlowTests(TestContext testContext) : EndToEndTestBase
{
    /// <summary>
    /// End-to-end smoke test of the reconfirm fan-out shape.
    ///
    /// The cron-driven <c>RequestReconfirmationsJob</c> ultimately schedules a
    /// system-triggered <c>BulkEmailJob</c> over the same
    /// reconfirmation shape that we exercise here directly. The request targets
    /// all attendees so delivery-time suppression, rather than only recipient
    /// resolution, is observable through the public HTTP/email path.
    ///
    /// Cron-trigger plumbing itself is covered by the Email module unit tests
    /// in section 7.4.
    /// </summary>
    // Given registrations that are registered-and-not-reconfirmed, already reconfirmed, and cancelled
    // When a reconfirm bulk email is created for all attendees
    // Then only the live registered, not-yet-reconfirmed attendee receives the email
    [TestMethod]
    public async Task ReconfirmFanOut_OnlyMailsRegisteredAndNotReconfirmedAttendees()
    {
        var fixture = BulkEmailFixture.Empty()
            .WithRegistration("needs-reconfirm@example.com", "Needs", "Reconfirm")
            .WithRegistration("already-reconfirmed@example.com", "Already", "Reconfirmed", reconfirmed: true)
            .WithRegistration("cancelled@example.com", "Was", "Cancelled", cancelled: true);
        await fixture.SetupAsync(Environment);
        var createResponse = await Environment.ApiClient.PostAsJsonAsync(
            fixture.CreateRoute,
            new
            {
                EmailType = "Reconfirmation",
                Subject = "Please reconfirm {{ first_name }}",
                TextBody = "Please reconfirm {{ first_name }}: {{ reconfirm_link }}",
                HtmlBody = "<p>Please reconfirm {{ first_name }}: {{ reconfirm_link }}</p>",
                AttendeeFilter = new { }
            },
            cancellationToken: testContext.CancellationToken);
        createResponse.StatusCode.ShouldBe(HttpStatusCode.Created);
        var createBody = await createResponse.Content.ReadFromJsonAsync<JsonElement>(
            cancellationToken: testContext.CancellationToken);
        var jobDetail = await fixture.PollUntilTerminalAsync(
            createBody.GetProperty("bulkEmailJobId").GetGuid(),
            Environment,
            testContext.CancellationToken);
        jobDetail.GetProperty("status").GetString().ShouldBe("completed");
        jobDetail.GetProperty("recipientCount").GetInt32().ShouldBe(3);
        jobDetail.GetProperty("sentCount").GetInt32().ShouldBe(1);

        var emails = await Environment.Email.WaitForAsync(
            expectedCount: 1,
            timeout: TimeSpan.FromSeconds(90),
            testContext.CancellationToken);

        // Wait an extra moment for any stragglers — the assertion below catches
        // accidental over-fanning.
        await Task.Delay(TimeSpan.FromSeconds(2), testContext.CancellationToken);
        var response = await Environment.Email.Client.GetAsync(
            "/api/email", testContext.CancellationToken);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>(
            cancellationToken: testContext.CancellationToken);
        emails = json.EnumerateArray().ToList();

        EmailTestContext.GetLowercaseRecipientAddresses(emails).ShouldBe(["needs-reconfirm@example.com"]);
        emails.Single().GetProperty("text").GetString()!.ShouldContain("/reconfirm/");
    }
}
