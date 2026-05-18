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
    /// SC-8.5: end-to-end smoke test of the reconfirm fan-out shape.
    ///
    /// The cron-driven <c>RequestReconfirmationsJob</c> ultimately schedules a
    /// system-triggered <c>BulkEmailJob</c> over the same
    /// <c>AttendeeSource(filter: Registered + HasReconfirmed=false, type: reconfirm)</c>
    /// shape that we exercise here directly. Driving the cron from outside the
    /// worker is impractical (it ticks at 09:00 in the event time zone), so we
    /// schedule the equivalent job through the public create endpoint and
    /// assert that fan-out only mails the un-reconfirmed registered attendee.
    ///
    /// Cron-trigger plumbing itself is covered by the Email module unit tests
    /// in section 7.4.
    /// </summary>
    [TestMethod]
    public async Task ReconfirmFanOut_OnlyMailsRegisteredAndNotReconfirmedAttendees()
    {
        var fixture = BulkEmailFixture.Empty()
            .WithReconfirmTemplate()
            .WithRegistration("needs-reconfirm@example.com", "Needs", "Reconfirm")
            .WithRegistration("already-reconfirmed@example.com", "Already", "Reconfirmed", reconfirmed: true)
            .WithRegistration("cancelled@example.com", "Was", "Cancelled", cancelled: true);
        await fixture.SetupAsync(Environment);

        var createResponse = await Environment.ApiClient.PostAsJsonAsync(
            fixture.CreateRoute,
            new
            {
                EmailType = BulkEmailFixture.ReconfirmEmailType,
                Source = new
                {
                    Attendee = new
                    {
                        RegistrationStatus = "registered",
                        HasReconfirmed = false
                    }
                }
            },
            cancellationToken: testContext.CancellationToken);
        createResponse.StatusCode.ShouldBe(HttpStatusCode.Created);

        var emails = await Environment.Email.WaitForAsync(
            expectedCount: 1,
            timeout: TimeSpan.FromSeconds(90),
            testContext.CancellationToken);

        // Wait an extra moment for any stragglers — the assertion below catches
        // accidental over-fanning.
        await Task.Delay(TimeSpan.FromSeconds(2), testContext.CancellationToken);
        var response = await Environment.Email.Client.GetAsync(
            "/email", testContext.CancellationToken);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>(
            cancellationToken: testContext.CancellationToken);
        emails = json.EnumerateArray().ToList();

        EmailTestContext.GetLowercaseRecipientAddresses(emails).ShouldBe(["needs-reconfirm@example.com"]);
    }
}
