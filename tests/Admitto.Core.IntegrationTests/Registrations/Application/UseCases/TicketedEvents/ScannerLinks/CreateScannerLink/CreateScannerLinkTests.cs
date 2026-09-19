using Amolenk.Admitto.Core.Registrations.Application.ScannerLinks;
using Amolenk.Admitto.Core.Registrations.Application.UseCases.TicketedEvents.ScannerLinks.CreateScannerLink;
using Amolenk.Admitto.Core.Registrations.Domain.Entities;
using Amolenk.Admitto.Core.Shared.Kernel.ErrorHandling;
using Amolenk.Admitto.Testing.Infrastructure.Assertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Amolenk.Admitto.Core.IntegrationTests.Registrations.Application.UseCases.TicketedEvents.ScannerLinks.CreateScannerLink;

[TestClass]
public sealed class CreateScannerLinkTests(TestContext testContext) : AspireIntegrationTestBase
{
    private static CreateScannerLinkHandler Handler() => new(
        Environment.RegistrationsDatabase.Context,
        TimeProvider.System,
        Options.Create(new ScannerLinksOptions { BaseUrl = "https://scanner.example/" }));

    // Given an active event without a scanner link
    // When a scanner link is created
    // Then an active shareable URL is returned and persisted
    [TestMethod]
    public async ValueTask CreateScannerLink_ActiveEvent_ReturnsActiveUrl()
    {
        var fixture = CreateScannerLinkFixture.ActiveEvent();
        await fixture.SetupAsync(Environment);

        var result = await Handler().HandleAsync(
            new CreateScannerLinkCommand(fixture.TeamId.Value, fixture.EventId.Value), testContext.CancellationToken);

        result.Status.ShouldBe("Active");
        result.Url.ShouldStartWith("https://scanner.example/scan/");
    }

    // Given an active event without a scanner link
    // When a scanner link is created
    // Then the link and the event's own LastChangedAt/LastChangedBy metadata are persisted
    [TestMethod]
    public async ValueTask CreateScannerLink_ActiveEvent_PersistsLinkAndBumpsAuditMetadata()
    {
        var fixture = CreateScannerLinkFixture.ActiveEvent();
        await fixture.SetupAsync(Environment);

        await Handler().HandleAsync(
            new CreateScannerLinkCommand(fixture.TeamId.Value, fixture.EventId.Value), testContext.CancellationToken);

        await Environment.RegistrationsDatabase.AssertAsync(async dbContext =>
        {
            var ticketedEvent = await dbContext.TicketedEvents
                .SingleAsync(e => e.Id == fixture.EventId, testContext.CancellationToken);

            ticketedEvent.ScannerLink.ShouldNotBeNull();
            ticketedEvent.ScannerLink!.Secret.ShouldNotBeNull();
            ticketedEvent.ScannerLink!.RevokedAt.ShouldBeNull();
            ticketedEvent.LastChangedAt.ShouldNotBe(default);
        });
    }

    // Given an event that already has a scanner link
    // When another scanner link is created
    // Then the already-exists error is thrown
    [TestMethod]
    public async ValueTask CreateScannerLink_ExistingLink_ThrowsAlreadyExists()
    {
        var fixture = CreateScannerLinkFixture.ExistingLink();
        await fixture.SetupAsync(Environment);

        var result = await ErrorResult.CaptureAsync(async () => { await Handler().HandleAsync(
            new CreateScannerLinkCommand(fixture.TeamId.Value, fixture.EventId.Value), testContext.CancellationToken); });

        result.Error.ShouldMatch(TicketedEvent.Errors.ScannerLinkAlreadyExists);
    }

    // Given an archived event
    // When a scanner link is created
    // Then the event-not-active error is thrown
    [TestMethod]
    public async ValueTask CreateScannerLink_ArchivedEvent_ThrowsEventNotActive()
    {
        var fixture = CreateScannerLinkFixture.ArchivedEvent();
        await fixture.SetupAsync(Environment);

        var result = await ErrorResult.CaptureAsync(async () => { await Handler().HandleAsync(
            new CreateScannerLinkCommand(fixture.TeamId.Value, fixture.EventId.Value), testContext.CancellationToken); });

        result.Error.ShouldMatch(TicketedEvent.Errors.EventNotActive);
    }

    // Given no event exists for the requested team
    // When a scanner link is created
    // Then a not-found error is thrown
    [TestMethod]
    public async ValueTask CreateScannerLink_WrongTeamOrEvent_ThrowsNotFound()
    {
        var fixture = CreateScannerLinkFixture.NoEvent();
        await fixture.SetupAsync(Environment);

        var result = await ErrorResult.CaptureAsync(async () => { await Handler().HandleAsync(
            new CreateScannerLinkCommand(fixture.TeamId.Value, fixture.EventId.Value), testContext.CancellationToken); });

        result.Error.ShouldMatch(NotFoundError.Create<TicketedEvent>());
    }
}
