using Amolenk.Admitto.Core.Registrations.Domain.Entities;
using Amolenk.Admitto.Core.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Core.IntegrationTests.Registrations.Application.UseCases.TicketedEvents.ScannerLinks.RegenerateScannerLink;

internal sealed class RegenerateScannerLinkFixture
{
    private readonly bool _withLink;
    private readonly bool _revoked;
    private readonly bool _archived;

    public TicketedEventId EventId { get; } = TicketedEventId.New();
    public TeamId TeamId { get; } = TeamId.New();

    private RegenerateScannerLinkFixture(bool withLink = true, bool revoked = false, bool archived = false)
    {
        _withLink = withLink;
        _revoked = revoked;
        _archived = archived;
    }

    public static RegenerateScannerLinkFixture ActiveLink() => new();
    public static RegenerateScannerLinkFixture RevokedLink() => new(revoked: true);
    public static RegenerateScannerLinkFixture NoLink() => new(withLink: false);
    public static RegenerateScannerLinkFixture ArchivedEvent() => new(archived: true);

    public async ValueTask SetupAsync(IntegrationTestEnvironment environment)
    {
        await environment.RegistrationsDatabase.SeedAsync(db =>
        {
            var ticketedEvent = TicketedEvent.Create(
                CreationRequestId.From(Guid.NewGuid()), EventId, TeamId,
                EventName.From("Scanner Event"), AbsoluteUrl.From("https://example.com"),
                AbsoluteUrl.From("https://tickets.example.com"), DateTimeOffset.UtcNow.AddDays(1),
                DateTimeOffset.UtcNow.AddDays(2), TimeZoneId.From("UTC"));

            if (_withLink)
            {
                ticketedEvent.CreateScannerLink(ScannerLinkSecret.From("old-secret"), DateTimeOffset.UtcNow);
                if (_revoked) ticketedEvent.RevokeScannerLink(DateTimeOffset.UtcNow);
            }
            if (_archived) ticketedEvent.Archive();

            db.TicketedEvents.Add(ticketedEvent);
        });
    }
}
