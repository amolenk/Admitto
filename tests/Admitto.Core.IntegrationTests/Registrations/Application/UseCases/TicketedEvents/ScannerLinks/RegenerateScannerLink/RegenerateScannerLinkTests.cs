using Amolenk.Admitto.Core.Registrations.Application.ScannerLinks;
using Amolenk.Admitto.Core.Registrations.Application.UseCases.TicketedEvents.ScannerLinks.RegenerateScannerLink;
using Amolenk.Admitto.Core.Registrations.Domain.Entities;
using Amolenk.Admitto.Testing.Infrastructure.Assertions;
using Microsoft.Extensions.Options;

namespace Amolenk.Admitto.Core.IntegrationTests.Registrations.Application.UseCases.TicketedEvents.ScannerLinks.RegenerateScannerLink;

[TestClass]
public sealed class RegenerateScannerLinkTests(TestContext testContext) : AspireIntegrationTestBase
{
    private static RegenerateScannerLinkHandler Handler() => new(
        Environment.RegistrationsDatabase.Context,
        TimeProvider.System,
        Options.Create(new ScannerLinksOptions { BaseUrl = "https://scanner.example" }));

    // Given an active event with a scanner link
    // When the scanner link is regenerated
    // Then a different active URL is returned
    [TestMethod]
    public async ValueTask RegenerateScannerLink_ActiveLink_ReturnsNewUrl()
    {
        var fixture = RegenerateScannerLinkFixture.ActiveLink();
        await fixture.SetupAsync(Environment);
        var old = "https://scanner.example/scan/old-secret";

        var result = await Handler().HandleAsync(
            new RegenerateScannerLinkCommand(fixture.TeamId.Value, fixture.EventId.Value), testContext.CancellationToken);

        result.Status.ShouldBe("Active");
        result.Url.ShouldNotBe(old);
    }

    // Given a revoked scanner link
    // When the scanner link is regenerated
    // Then a new active link is returned
    [TestMethod]
    public async ValueTask RegenerateScannerLink_RevokedLink_ReturnsActiveLink()
    {
        var fixture = RegenerateScannerLinkFixture.RevokedLink();
        await fixture.SetupAsync(Environment);

        var result = await Handler().HandleAsync(
            new RegenerateScannerLinkCommand(fixture.TeamId.Value, fixture.EventId.Value), testContext.CancellationToken);

        result.Status.ShouldBe("Active");
        result.Url.ShouldNotBeNull();
    }

    // Given an event without a scanner link
    // When the scanner link is regenerated
    // Then a not-found scanner-link error is thrown
    [TestMethod]
    public async ValueTask RegenerateScannerLink_NoLink_ThrowsNotFound()
    {
        var fixture = RegenerateScannerLinkFixture.NoLink();
        await fixture.SetupAsync(Environment);

        var result = await ErrorResult.CaptureAsync(async () => { await Handler().HandleAsync(
            new RegenerateScannerLinkCommand(fixture.TeamId.Value, fixture.EventId.Value), testContext.CancellationToken); });

        result.Error.ShouldMatch(TicketedEvent.Errors.ScannerLinkNotFound);
    }

    // Given an archived event
    // When the scanner link is regenerated
    // Then the event-not-active error is thrown
    [TestMethod]
    public async ValueTask RegenerateScannerLink_ArchivedEvent_ThrowsEventNotActive()
    {
        var fixture = RegenerateScannerLinkFixture.ArchivedEvent();
        await fixture.SetupAsync(Environment);

        var result = await ErrorResult.CaptureAsync(async () => { await Handler().HandleAsync(
            new RegenerateScannerLinkCommand(fixture.TeamId.Value, fixture.EventId.Value), testContext.CancellationToken); });

        result.Error.ShouldMatch(TicketedEvent.Errors.EventNotActive);
    }
}
