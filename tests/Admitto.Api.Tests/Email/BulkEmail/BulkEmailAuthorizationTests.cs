using System.Net;
using System.Net.Http.Json;
using Amolenk.Admitto.Api.Tests.Infrastructure;
using Shouldly;

namespace Amolenk.Admitto.Api.Tests.Email.BulkEmail;

[TestClass]
public sealed class BulkEmailAuthorizationTests(TestContext testContext) : EndToEndTestBase
{
    // SC-8.6: every bulk-email admin endpoint requires Organizer team membership.
    // Bob is authenticated but not a member of the team — every call must 403.
    [TestMethod]
    public async Task NonOrganizer_GetsForbiddenOnEveryEndpoint()
    {
        var fixture = BulkEmailFixture.Empty();
        await fixture.SetupAsync(Environment);

        var preview = await Environment.BobApiClient.PostAsJsonAsync(
            fixture.PreviewRoute,
            new
            {
                Source = new
                {
                    ExternalList = new
                    {
                        Items = new[] { new { Email = "x@example.com", DisplayName = (string?)null } }
                    }
                }
            },
            cancellationToken: testContext.CancellationToken);
        preview.StatusCode.ShouldBe(HttpStatusCode.Forbidden);

        var create = await Environment.BobApiClient.PostAsJsonAsync(
            fixture.CreateRoute,
            new
            {
                EmailType = "bulk-custom",
                Subject = "Hello",
                TextBody = "Hello",
                HtmlBody = "<p>Hello</p>",
                Source = new
                {
                    ExternalList = new
                    {
                        Items = new[] { new { Email = "x@example.com", DisplayName = (string?)null } }
                    }
                }
            },
            cancellationToken: testContext.CancellationToken);
        create.StatusCode.ShouldBe(HttpStatusCode.Forbidden);

        var list = await Environment.BobApiClient.GetAsync(
            fixture.ListRoute, testContext.CancellationToken);
        list.StatusCode.ShouldBe(HttpStatusCode.Forbidden);

        var arbitraryId = Guid.NewGuid();
        var detail = await Environment.BobApiClient.GetAsync(
            fixture.DetailRoute(arbitraryId), testContext.CancellationToken);
        detail.StatusCode.ShouldBe(HttpStatusCode.Forbidden);

        var cancel = await Environment.BobApiClient.PostAsync(
            fixture.CancelRoute(arbitraryId),
            content: null,
            cancellationToken: testContext.CancellationToken);
        cancel.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }
}
