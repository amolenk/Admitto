using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Amolenk.Admitto.Api.Tests.Infrastructure;
using Shouldly;

namespace Amolenk.Admitto.Api.Tests.Email.BulkEmail;

[TestClass]
public sealed class BulkEmailPreviewTests(TestContext testContext) : EndToEndTestBase
{
    // SC-8.1: POST /preview returns expected count + sample for the attendee source shape.
    [TestMethod]
    public async Task SC001_Preview_AttendeeSource_ReturnsCountAndSample()
    {
        var fixture = BulkEmailFixture.Empty()
            .WithRegistration("alice@example.com", "Alice", "Anderson")
            .WithRegistration("bob@example.com", "Bob", "Brown")
            .WithRegistration("carol@example.com", "Carol", "Clark", reconfirmed: true);
        await fixture.SetupAsync(Environment);

        var request = new
        {
            Source = new
            {
                Attendee = new { HasReconfirmed = false }
            }
        };

        var response = await Environment.ApiClient.PostAsJsonAsync(
            BulkEmailFixture.PreviewRoute,
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

    // SC-8.1: POST /preview returns expected count + sample for the external-list source shape.
    [TestMethod]
    public async Task SC002_Preview_ExternalListSource_ReturnsCountAndSample()
    {
        var fixture = BulkEmailFixture.Empty();
        await fixture.SetupAsync(Environment);

        var request = new
        {
            Source = new
            {
                ExternalList = new
                {
                    Items = new[]
                    {
                        new { Email = "external1@example.com", DisplayName = "Ext One" },
                        new { Email = "external2@example.com", DisplayName = (string?)null! }
                    }
                }
            }
        };

        var response = await Environment.ApiClient.PostAsJsonAsync(
            BulkEmailFixture.PreviewRoute,
            request,
            cancellationToken: testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>(
            cancellationToken: testContext.CancellationToken);
        body.GetProperty("count").GetInt32().ShouldBe(2);

        var sample = body.GetProperty("sample").EnumerateArray().ToList();
        sample.Count.ShouldBe(2);
        sample.Select(s => s.GetProperty("email").GetString())
            .ShouldBe(new[] { "external1@example.com", "external2@example.com" }, ignoreOrder: true);
    }

    // Validator: requests carrying both source shapes must be rejected.
    [TestMethod]
    public async Task SC003_Preview_BothSourcesProvided_ReturnsBadRequest()
    {
        var fixture = BulkEmailFixture.Empty();
        await fixture.SetupAsync(Environment);

        var request = new
        {
            Source = new
            {
                Attendee = new { },
                ExternalList = new
                {
                    Items = new[] { new { Email = "x@example.com", DisplayName = (string?)null } }
                }
            }
        };

        var response = await Environment.ApiClient.PostAsJsonAsync(
            BulkEmailFixture.PreviewRoute,
            request,
            cancellationToken: testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }
}
