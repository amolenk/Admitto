using Amolenk.Admitto.Core.Registrations.Domain.Entities;
using Amolenk.Admitto.Core.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Core.IntegrationTests.Registrations.Application.UseCases.TicketedEvents.ScannerLinks.RevokeScannerLink;

internal sealed class RevokeScannerLinkFixture
{
    private readonly bool _withLink;
    private readonly bool _revoked;

    public TicketedEventId EventId { get; } = TicketedEventId.New();
    public TeamId TeamId { get; } = TeamId.New();

    private RevokeScannerLinkFixture(bool withLink = true, bool revoked = false)
    {
        _withLink = withLink;
        _revoked = revoked;
    }

    public static RevokeScannerLinkFixture ActiveLink() => new();
    public static RevokeScannerLinkFixture NoLink() => new(withLink: false);
    public static RevokeScannerLinkFixture RevokedLink() => new(revoked: true);

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
                ticketedEvent.CreateScannerLink(ScannerLinkSecret.From("secret"), DateTimeOffset.UtcNow);
                if (_revoked) ticketedEvent.RevokeScannerLink(DateTimeOffset.UtcNow);
            }

            db.TicketedEvents.Add(ticketedEvent);
        });
    }
}
