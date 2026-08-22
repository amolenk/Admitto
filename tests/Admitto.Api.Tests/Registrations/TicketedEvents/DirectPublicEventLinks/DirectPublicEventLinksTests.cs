using System.Net;
using Amolenk.Admitto.Api.Tests.Infrastructure;
using Shouldly;

namespace Amolenk.Admitto.Api.Tests.Registrations.TicketedEvents.DirectPublicEventLinks;

[TestClass]
public sealed class DirectPublicEventLinksTests(TestContext testContext) : EndToEndTestBase
{
    // Given a ticketed event published with a public slug and website URL
    // When the public event link is requested with an attacker-controlled redirect query parameter
    // Then the API redirects to the event's configured website URL, ignoring the query parameter
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

    // Given a ticketed event published with a public slug and website URL
    // When the public register link is requested
    // Then the API redirects to the website's register path
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

    // Given a ticketed event published with a public slug and website URL, and a registration
    // When the public cancel link is requested for that registration
    // Then the API redirects to the website's cancel path for that registration
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

    // Given a ticketed event published with a public slug and website URL, and a registration
    // When the public edit link is requested for that registration
    // Then the API redirects to the website's edit path for that registration
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

    // Given no ticketed event exists with the given public slug
    // When a public register link is requested for that unknown slug
    // Then the API returns 404 Not Found without a redirect location
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
