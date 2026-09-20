using Amolenk.Admitto.Core.Registrations.Domain.Entities;
using Amolenk.Admitto.Core.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Core.IntegrationTests.Registrations.Application.UseCases.TicketedEvents.ScannerLinks.GetScannerLink;

internal sealed class GetScannerLinkFixture
{
    private readonly bool _withLink;
    private readonly bool _expired;
    private readonly bool _revoked;
    private readonly bool _archived;

    public TicketedEventId EventId { get; } = TicketedEventId.New();
    public TeamId TeamId { get; } = TeamId.New();

    private GetScannerLinkFixture(bool withLink = false, bool expired = false, bool revoked = false, bool archived = false)
    {
        _withLink = withLink;
        _expired = expired;
        _revoked = revoked;
        _archived = archived;
    }

    public static GetScannerLinkFixture NoLink() => new();
    public static GetScannerLinkFixture ActiveLink() => new(withLink: true);
    public static GetScannerLinkFixture ExpiredLink() => new(withLink: true, expired: true);
    public static GetScannerLinkFixture RevokedLink() => new(withLink: true, revoked: true);
    public static GetScannerLinkFixture ArchivedEventWithLink() => new(withLink: true, archived: true);

    public async ValueTask SetupAsync(IntegrationTestEnvironment environment)
    {
        await environment.RegistrationsDatabase.SeedAsync(db =>
        {
            var endsAt = _expired ? DateTimeOffset.UtcNow.AddMinutes(-1) : DateTimeOffset.UtcNow.AddDays(2);
            var ticketedEvent = TicketedEvent.Create(
                CreationRequestId.From(Guid.NewGuid()), EventId, TeamId,
                EventName.From("Scanner Event"), AbsoluteUrl.From("https://example.com"),
                AbsoluteUrl.From("https://tickets.example.com"), DateTimeOffset.UtcNow.AddDays(-1),
                endsAt, TimeZoneId.From("UTC"));

            if (_withLink)
            {
                ticketedEvent.CreateScannerLink(ScannerLinkSecret.From("secret"), DateTimeOffset.UtcNow);
                if (_revoked) ticketedEvent.RevokeScannerLink(DateTimeOffset.UtcNow);
            }

            // Archiving happens after the link is created, since creation requires an Active event.
            if (_archived) ticketedEvent.Archive();

            db.TicketedEvents.Add(ticketedEvent);
        });
    }
}
