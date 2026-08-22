using System.Net;
using System.Net.Http.Json;
using Amolenk.Admitto.Api.Tests.Infrastructure;
using Shouldly;

namespace Amolenk.Admitto.Api.Tests.Email.BulkEmail;

[TestClass]
public sealed class BulkEmailAuthorizationTests(TestContext testContext) : EndToEndTestBase
{
    // Every bulk-email admin endpoint requires Organizer team membership.
    // Given a user who is authenticated but not a member of the team
    // When they call every bulk-email admin endpoint (preview, create, list, detail, cancel)
    // Then each call returns 403 Forbidden
    [TestMethod]
    public async Task NonOrganizer_GetsForbiddenOnEveryEndpoint()
    {
        var fixture = BulkEmailFixture.Empty();
        await fixture.SetupAsync(Environment);

        var preview = await Environment.BobApiClient.PostAsJsonAsync(
            fixture.PreviewRoute,
            new
            {
                AttendeeFilter = new { }
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
                AttendeeFilter = new { }
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
