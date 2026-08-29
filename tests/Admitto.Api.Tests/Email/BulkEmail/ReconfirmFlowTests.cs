using System.Net;
using System.Net.Http.Json;
using Amolenk.Admitto.Api.Tests.Infrastructure;
using Shouldly;

namespace Amolenk.Admitto.Api.Tests.Email.BulkEmail;

[TestClass]
public sealed class ReconfirmFlowTests(TestContext testContext) : EndToEndTestBase
{
    // Given a generic bulk-email create request using the reserved reconfirmation type
    // When the request is validated
    // Then it is rejected because reconfirmation belongs to the hourly batch flow
    [TestMethod]
    public async Task Create_ReconfirmationEmailType_IsRejected()
    {
        var fixture = BulkEmailFixture.Empty();
        await fixture.SetupAsync(Environment);

        var response = await Environment.ApiClient.PostAsJsonAsync(
            fixture.CreateRoute,
            new
            {
                EmailType = "Reconfirmation",
                Subject = "Please reconfirm",
                TextBody = "Please reconfirm",
                HtmlBody = "<p>Please reconfirm</p>",
                AttendeeFilter = new { }
            },
            cancellationToken: testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    // Given a generic bulk-email preview request using the reserved reconfirmation type
    // When the request is validated
    // Then it is rejected before resolving recipients
    [TestMethod]
    public async Task Preview_ReconfirmationEmailType_IsRejected()
    {
        var fixture = BulkEmailFixture.Empty();
        await fixture.SetupAsync(Environment);

        var response = await Environment.ApiClient.PostAsJsonAsync(
            fixture.PreviewRoute,
            new
            {
                EmailType = "Reconfirmation",
                AttendeeFilter = new { }
            },
            cancellationToken: testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }
}
