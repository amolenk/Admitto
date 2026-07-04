using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Amolenk.Admitto.Api.Tests.Infrastructure;
using Shouldly;

namespace Amolenk.Admitto.Api.Tests.Email.BulkEmail;

[TestClass]
public sealed class BulkEmailPreviewTests(TestContext testContext) : EndToEndTestBase
{
    // POST /preview returns expected count + sample for the attendee filter.
    [TestMethod]
    public async Task Preview_AttendeeFilter_ReturnsCountAndSample()
    {
        var fixture = BulkEmailFixture.Empty()
            .WithRegistration("alice@example.com", "Alice", "Anderson")
            .WithRegistration("bob@example.com", "Bob", "Brown")
            .WithRegistration("carol@example.com", "Carol", "Clark", reconfirmed: true);
        await fixture.SetupAsync(Environment);

        var request = new
        {
            AttendeeFilter = new { HasReconfirmed = false }
        };

        var response = await Environment.ApiClient.PostAsJsonAsync(
            fixture.PreviewRoute,
            request,
            cancellationToken: testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>(
            cancellationToken: testContext.CancellationToken);
        body.GetProperty("count").GetInt32().ShouldBe(2);

        var sampleEmails = body.GetProperty("sample")
            .EnumerateArray()
            .Select(s => s.GetProperty("email").GetString()!)
            .ToList();
        sampleEmails.ShouldBe(new[] { "alice@example.com", "bob@example.com" }, ignoreOrder: true);
    }
}
