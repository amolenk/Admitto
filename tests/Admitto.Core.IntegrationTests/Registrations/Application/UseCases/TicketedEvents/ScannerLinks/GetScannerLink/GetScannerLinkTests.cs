using Amolenk.Admitto.Core.Registrations.Application.ScannerLinks;
using Amolenk.Admitto.Core.Registrations.Application.UseCases.TicketedEvents.ScannerLinks.GetScannerLink;
using Microsoft.Extensions.Options;

namespace Amolenk.Admitto.Core.IntegrationTests.Registrations.Application.UseCases.TicketedEvents.ScannerLinks.GetScannerLink;

[TestClass]
public sealed class GetScannerLinkTests(TestContext testContext) : AspireIntegrationTestBase
{
    private static GetScannerLinkHandler Handler() => new(
        Environment.RegistrationsDatabase.Context,
        TimeProvider.System,
        Options.Create(new ScannerLinksOptions { BaseUrl = "https://scanner.example/" }));

    // Given an event without a scanner link
    // When its scanner link is requested
    // Then the status is None
    [TestMethod]
    public async ValueTask GetScannerLink_NoLink_ReturnsNone()
    {
        var fixture = GetScannerLinkFixture.NoLink();
        await fixture.SetupAsync(Environment);

        var result = await Handler().HandleAsync(
            new GetScannerLinkQuery(fixture.TeamId.Value, fixture.EventId.Value), testContext.CancellationToken);

        result.ShouldNotBeNull();
        result.Status.ShouldBe("None");
        result.Url.ShouldBeNull();
    }

    // Given an active scanner link
    // When its scanner link is requested
    // Then the status is Active and a URL is returned
    [TestMethod]
    public async ValueTask GetScannerLink_ActiveLink_ReturnsActiveUrl()
    {
        var fixture = GetScannerLinkFixture.ActiveLink();
        await fixture.SetupAsync(Environment);

        var result = await Handler().HandleAsync(
            new GetScannerLinkQuery(fixture.TeamId.Value, fixture.EventId.Value), testContext.CancellationToken);

        result.ShouldNotBeNull();
        result.Status.ShouldBe("Active");
        result.Url.ShouldBe("https://scanner.example/scan/secret");
    }

    // Given a scanner link whose event has ended
    // When its scanner link is requested
    // Then the status is Expired without a URL
    [TestMethod]
    public async ValueTask GetScannerLink_EndedEvent_ReturnsExpired()
    {
        var fixture = GetScannerLinkFixture.ExpiredLink();
        await fixture.SetupAsync(Environment);

        var result = await Handler().HandleAsync(
            new GetScannerLinkQuery(fixture.TeamId.Value, fixture.EventId.Value), testContext.CancellationToken);

        result.ShouldNotBeNull();
        result.Status.ShouldBe("Expired");
        result.Url.ShouldBeNull();
    }

    // Given a revoked scanner link
    // When its scanner link is requested
    // Then the status is Revoked without a URL
    [TestMethod]
    public async ValueTask GetScannerLink_RevokedLink_ReturnsRevoked()
    {
        var fixture = GetScannerLinkFixture.RevokedLink();
        await fixture.SetupAsync(Environment);

        var result = await Handler().HandleAsync(
            new GetScannerLinkQuery(fixture.TeamId.Value, fixture.EventId.Value), testContext.CancellationToken);

        result.ShouldNotBeNull();
        result.Status.ShouldBe("Revoked");
        result.Url.ShouldBeNull();
    }

    // Given an event that was archived after its scanner link was created
    // When its scanner link is requested
    // Then the status is Expired without a URL, even though the link's own timers say it's still valid
    [TestMethod]
    public async ValueTask GetScannerLink_ArchivedEvent_ReturnsExpiredWithoutUrl()
    {
        var fixture = GetScannerLinkFixture.ArchivedEventWithLink();
        await fixture.SetupAsync(Environment);

        var result = await Handler().HandleAsync(
            new GetScannerLinkQuery(fixture.TeamId.Value, fixture.EventId.Value), testContext.CancellationToken);

        result.ShouldNotBeNull();
        result.Status.ShouldBe("Expired");
        result.Url.ShouldBeNull();
    }
}
