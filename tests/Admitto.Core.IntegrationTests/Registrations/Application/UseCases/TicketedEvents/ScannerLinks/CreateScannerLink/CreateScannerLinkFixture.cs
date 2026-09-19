using Amolenk.Admitto.Core.Registrations.Domain.Entities;
using Amolenk.Admitto.Core.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Core.IntegrationTests.Registrations.Application.UseCases.TicketedEvents.ScannerLinks.CreateScannerLink;

internal sealed class CreateScannerLinkFixture
{
    private readonly bool _withLink;
    private readonly bool _archived;
    private readonly bool _withEvent;

    public TicketedEventId EventId { get; } = TicketedEventId.New();
    public TeamId TeamId { get; } = TeamId.New();

    private CreateScannerLinkFixture(bool withLink = false, bool archived = false, bool withEvent = true)
    {
        _withLink = withLink;
        _archived = archived;
        _withEvent = withEvent;
    }

    public static CreateScannerLinkFixture ActiveEvent() => new();
    public static CreateScannerLinkFixture ExistingLink() => new(withLink: true);
    public static CreateScannerLinkFixture ArchivedEvent() => new(archived: true);
    public static CreateScannerLinkFixture NoEvent() => new(withEvent: false);

    public async ValueTask SetupAsync(IntegrationTestEnvironment environment)
    {
        if (!_withEvent) return;

        await environment.RegistrationsDatabase.SeedAsync(db =>
        {
            var ticketedEvent = TicketedEvent.Create(
                CreationRequestId.From(Guid.NewGuid()), EventId, TeamId,
                EventName.From("Scanner Event"), AbsoluteUrl.From("https://example.com"),
                AbsoluteUrl.From("https://tickets.example.com"), DateTimeOffset.UtcNow.AddDays(1),
                DateTimeOffset.UtcNow.AddDays(2), TimeZoneId.From("UTC"));

            if (_withLink)
                ticketedEvent.CreateScannerLink(ScannerLinkSecret.From("existing-secret"), DateTimeOffset.UtcNow);
            if (_archived)
                ticketedEvent.Archive();

            db.TicketedEvents.Add(ticketedEvent);
        });
    }
}
