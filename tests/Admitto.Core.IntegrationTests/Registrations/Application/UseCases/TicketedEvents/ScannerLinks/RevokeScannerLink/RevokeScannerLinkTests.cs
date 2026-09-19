using Amolenk.Admitto.Core.Registrations.Application.ScannerLinks;
using Amolenk.Admitto.Core.Registrations.Application.UseCases.TicketedEvents.ScannerLinks.RevokeScannerLink;
using Amolenk.Admitto.Core.Registrations.Domain.Entities;
using Amolenk.Admitto.Testing.Infrastructure.Assertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Amolenk.Admitto.Core.IntegrationTests.Registrations.Application.UseCases.TicketedEvents.ScannerLinks.RevokeScannerLink;

[TestClass]
public sealed class RevokeScannerLinkTests(TestContext testContext) : AspireIntegrationTestBase
{
    private static RevokeScannerLinkHandler Handler() => new(
        Environment.RegistrationsDatabase.Context,
        TimeProvider.System,
        Options.Create(new ScannerLinksOptions { BaseUrl = "https://scanner.example" }));

    // Given an active scanner link
    // When the scanner link is revoked
    // Then the response is revoked and does not expose a URL
    [TestMethod]
    public async ValueTask RevokeScannerLink_ActiveLink_ReturnsRevoked()
    {
        var fixture = RevokeScannerLinkFixture.ActiveLink();
        await fixture.SetupAsync(Environment);

        var result = await Handler().HandleAsync(
            new RevokeScannerLinkCommand(fixture.TeamId.Value, fixture.EventId.Value), testContext.CancellationToken);

        result.Status.ShouldBe("Revoked");
        result.Url.ShouldBeNull();
    }

    // Given an active scanner link
    // When the scanner link is revoked
    // Then the secret is permanently cleared from storage, not merely hidden from responses
    [TestMethod]
    public async ValueTask RevokeScannerLink_ActiveLink_ClearsPersistedSecret()
    {
        var fixture = RevokeScannerLinkFixture.ActiveLink();
        await fixture.SetupAsync(Environment);

        await Handler().HandleAsync(
            new RevokeScannerLinkCommand(fixture.TeamId.Value, fixture.EventId.Value), testContext.CancellationToken);

        await Environment.RegistrationsDatabase.AssertAsync(async dbContext =>
        {
            var ticketedEvent = await dbContext.TicketedEvents
                .SingleAsync(e => e.Id == fixture.EventId, testContext.CancellationToken);

            ticketedEvent.ScannerLink.ShouldNotBeNull();
            ticketedEvent.ScannerLink!.Secret.ShouldBeNull();
            ticketedEvent.ScannerLink!.RevokedAt.ShouldNotBeNull();
        });
    }

    // Given an event without a scanner link
    // When the scanner link is revoked
    // Then a not-found scanner-link error is thrown
    [TestMethod]
    public async ValueTask RevokeScannerLink_NoLink_ThrowsNotFound()
    {
        var fixture = RevokeScannerLinkFixture.NoLink();
        await fixture.SetupAsync(Environment);

        var result = await ErrorResult.CaptureAsync(async () => { await Handler().HandleAsync(
            new RevokeScannerLinkCommand(fixture.TeamId.Value, fixture.EventId.Value), testContext.CancellationToken); });

        result.Error.ShouldMatch(TicketedEvent.Errors.ScannerLinkNotFound);
    }

    // Given an already revoked scanner link
    // When the scanner link is revoked again
    // Then the already-revoked error is thrown
    [TestMethod]
    public async ValueTask RevokeScannerLink_AlreadyRevoked_ThrowsAlreadyRevoked()
    {
        var fixture = RevokeScannerLinkFixture.RevokedLink();
        await fixture.SetupAsync(Environment);

        var result = await ErrorResult.CaptureAsync(async () => { await Handler().HandleAsync(
            new RevokeScannerLinkCommand(fixture.TeamId.Value, fixture.EventId.Value), testContext.CancellationToken); });

        result.Error.ShouldMatch(TicketedEvent.Errors.ScannerLinkAlreadyRevoked);
    }
}
