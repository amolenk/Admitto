using Amolenk.Admitto.Core.Registrations.Domain.Entities;
using Amolenk.Admitto.Core.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;
using Amolenk.Admitto.Core.Registrations.Contracts.ValueObjects;

namespace Amolenk.Admitto.Core.IntegrationTests.Registrations.Application.UseCases.Registrations.ChangeAttendeeTickets;

internal sealed class ChangeAttendeeTicketsFixture
{
    private readonly Dictionary<string, TicketTypeId> _ticketTypeIdsBySlug = new();
    private TicketedEvent? _ticketedEvent;
    private TicketCatalog? _catalog;
    private bool _preCancel;

    public TicketedEventId EventId { get; } = TicketedEventId.New();
    public TeamId TeamId { get; } = TeamId.New();
    public RegistrationId RegistrationId { get; private set; } = RegistrationId.New();

    public TicketTypeId GetTicketTypeId(string slug) => _ticketTypeIdsBySlug[slug];

    private ChangeAttendeeTicketsFixture() { }

    public static ChangeAttendeeTicketsFixture WithCapacity(int earlyBirdMax = 100, int earlyBirdUsed = 50,
        int workshopMax = 20, int workshopUsed = 10)
    {
        var f = new ChangeAttendeeTicketsFixture();
        f._ticketedEvent = f.MakeActiveEvent();
        f._catalog = f.MakeCatalog(
            ("early-bird", "Early Bird", earlyBirdMax, earlyBirdUsed),
            ("workshop", "Workshop", workshopMax, workshopUsed));
        return f;
    }

    public static ChangeAttendeeTicketsFixture WithCancelledRegistration()
    {
        var f = new ChangeAttendeeTicketsFixture { _preCancel = true };
        f._ticketedEvent = f.MakeActiveEvent();
        f._catalog = f.MakeCatalog(("early-bird", "Early Bird", 100, 50));
        return f;
    }

    public static ChangeAttendeeTicketsFixture WithArchivedEvent()
    {
        var f = new ChangeAttendeeTicketsFixture();
        var ev = f.MakeActiveEvent();
        ev.Archive();
        var catalog = f.MakeCatalog(("early-bird", "Early Bird", 100, 50));
        catalog.MarkEventArchived();
        f._ticketedEvent = ev;
        f._catalog = catalog;
        return f;
    }

    public static ChangeAttendeeTicketsFixture WithSoldOutWorkshop()
    {
        var f = new ChangeAttendeeTicketsFixture();
        f._ticketedEvent = f.MakeActiveEvent();
        f._catalog = f.MakeCatalog(
            ("early-bird", "Early Bird", 100, 50),
            ("workshop", "Workshop", 1, 1));
        return f;
    }

    public async ValueTask SetupAsync(IntegrationTestEnvironment environment)
    {
        await environment.RegistrationsDatabase.SeedAsync(dbContext =>
        {
            if (_ticketedEvent is not null) dbContext.TicketedEvents.Add(_ticketedEvent);
            if (_catalog is not null) dbContext.TicketCatalogs.Add(_catalog);

            var earlyBirdId = _ticketTypeIdsBySlug.TryGetValue("early-bird", out var id) ? id : TicketTypeId.New();
            var registration = Registration.Create(
                TeamId,
                EventId,
                EmailAddress.From("alice@example.com"),
                FirstName.From("Alice"),
                LastName.From("Test"),
                [new TicketTypeSnapshot(earlyBirdId, TicketTypeName.From("Early Bird"), [])]);
            RegistrationId = registration.Id;
            if (_preCancel) registration.Cancel(CancellationReason.AttendeeRequest);
            dbContext.Registrations.Add(registration);
        });
    }

    private TicketedEvent MakeActiveEvent() =>
        TicketedEvent.Create(
            CreationRequestId.From(Guid.NewGuid()),
            EventId,
            TeamId,
            EventName.From("DevConf"),
            AbsoluteUrl.From("https://example.com"),
            AbsoluteUrl.From("https://tickets.example.com"),
            DateTimeOffset.UtcNow.AddDays(60),
            DateTimeOffset.UtcNow.AddDays(61),
            TimeZoneId.From("UTC"));

    private TicketCatalog MakeCatalog(params (string slug, string name, int max, int used)[] ticketTypes)
    {
        var catalog = TicketCatalog.Create(EventId, TeamId);
        foreach (var (slug, name, max, used) in ticketTypes)
        {
            var id = TicketTypeId.New();
            _ticketTypeIdsBySlug[slug] = id;
            catalog.AddTicketType(id, TicketTypeName.From(name), [], max);
            for (var i = 0; i < used; i++) catalog.Claim([id], enforce: false);
        }
        return catalog;
    }
}
