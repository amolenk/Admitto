using System.Net;
using Amolenk.Admitto.Api.Tests.Infrastructure;
using Shouldly;

namespace Amolenk.Admitto.Api.Tests.Registrations.TicketedEvents.DirectPublicEventLinks;

[TestClass]
public sealed class DirectPublicEventLinksTests(TestContext testContext) : EndToEndTestBase
{
    [TestMethod]
    public async Task PublicEventLink_ExistingSlug_RedirectsToWebsiteUrl()
    {
        var fixture = DirectPublicEventLinksFixture.HappyFlow();
        await fixture.SetupAsync(Environment);

        using var client = CreateNoRedirectClient();
        var response = await client.GetAsync(
            fixture.PublicEventRoute() + "?redirect=https://attacker.example",
            testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Found);
        response.Headers.Location?.OriginalString.ShouldBe("https://partner.example.com");
    }

    [TestMethod]
    public async Task RegisterLink_ExistingSlug_RedirectsToWebsiteRegisterPath()
    {
        var fixture = DirectPublicEventLinksFixture.HappyFlow();
        await fixture.SetupAsync(Environment);

        using var client = CreateNoRedirectClient();
        var response = await client.GetAsync(fixture.RegisterRoute(), testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Found);
        response.Headers.Location?.ToString().ShouldBe("https://partner.example.com/tickets/register");
    }

    [TestMethod]
    public async Task CancelLink_ExistingSlug_RedirectsToWebsiteCancelPath()
    {
        var fixture = DirectPublicEventLinksFixture.HappyFlow();
        await fixture.SetupAsync(Environment);

        using var client = CreateNoRedirectClient();
        var response = await client.GetAsync(
            fixture.CancelRoute(fixture.RegistrationId),
            testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Found);
        response.Headers.Location?.ToString()
            .ShouldBe($"https://partner.example.com/tickets/cancel/{fixture.RegistrationId}");
    }

    [TestMethod]
    public async Task EditLink_ExistingSlug_RedirectsToWebsiteEditPath()
    {
        var fixture = DirectPublicEventLinksFixture.HappyFlow();
        await fixture.SetupAsync(Environment);

        using var client = CreateNoRedirectClient();
        var response = await client.GetAsync(
            fixture.EditRoute(fixture.RegistrationId),
            testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Found);
        response.Headers.Location?.ToString()
            .ShouldBe($"https://partner.example.com/tickets/edit/{fixture.RegistrationId}");
    }

    [TestMethod]
    public async Task DirectPublicLink_UnknownSlug_Returns404()
    {
        var fixture = DirectPublicEventLinksFixture.HappyFlow();
        await fixture.SetupAsync(Environment);

        using var client = CreateNoRedirectClient();
        var response = await client.GetAsync(
            fixture.RegisterRoute(publicSlug: "unknown-event"),
            testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        response.Headers.Location.ShouldBeNull();
    }

    private HttpClient CreateNoRedirectClient() =>
        new(new HttpClientHandler { AllowAutoRedirect = false })
        {
            BaseAddress = Environment.AnonymousApiClient.BaseAddress
        };
}
