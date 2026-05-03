using Amolenk.Admitto.Module.Registrations.Domain.Entities;
using Amolenk.Admitto.Module.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Module.Registrations.Tests.Application.Infrastructure.Hosting;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Module.Registrations.Tests.Application.UseCases.Registrations.ChangeAttendeeTickets;

internal sealed class ChangeAttendeeTicketsFixture
{
    private TicketedEvent? _ticketedEvent;
    private TicketCatalog? _catalog;
    private bool _preCancel;

    public TicketedEventId EventId { get; } = TicketedEventId.New();
    public TeamId TeamId { get; } = TeamId.New();
    public RegistrationId RegistrationId { get; private set; } = RegistrationId.New();

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

    public static ChangeAttendeeTicketsFixture WithCancelledEvent()
    {
        var f = new ChangeAttendeeTicketsFixture();
        var ev = f.MakeActiveEvent();
        ev.Cancel();
        f._ticketedEvent = ev;
        f._catalog = f.MakeCatalog(("early-bird", "Early Bird", 100, 50));
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
        await environment.Database.SeedAsync(dbContext =>
        {
            if (_ticketedEvent is not null) dbContext.TicketedEvents.Add(_ticketedEvent);
            if (_catalog is not null) dbContext.TicketCatalogs.Add(_catalog);

            var registration = Registration.Create(
                TeamId,
                EventId,
                EmailAddress.From("alice@example.com"),
                FirstName.From("Alice"),
                LastName.From("Test"),
                [new TicketTypeSnapshot("early-bird", "Early Bird", [])]);
            RegistrationId = registration.Id;
            if (_preCancel) registration.Cancel(CancellationReason.AttendeeRequest);
            dbContext.Registrations.Add(registration);
        });
    }

    private TicketedEvent MakeActiveEvent() =>
        TicketedEvent.Create(
            EventId,
            TeamId,
            Slug.From("test-team"),
            Slug.From("devconf"),
            DisplayName.From("DevConf"),
            AbsoluteUrl.From("https://example.com"),
            AbsoluteUrl.From("https://tickets.example.com"),
            DateTimeOffset.UtcNow.AddDays(60),
            DateTimeOffset.UtcNow.AddDays(61),
            TimeZoneId.From("UTC"));

    private TicketCatalog MakeCatalog(params (string slug, string name, int max, int used)[] ticketTypes)
    {
        var catalog = TicketCatalog.Create(EventId);
        foreach (var (slug, name, max, used) in ticketTypes)
        {
            catalog.AddTicketType(Slug.From(slug), DisplayName.From(name), [], max);
            for (var i = 0; i < used; i++) catalog.Claim([slug], enforce: false);
        }
        return catalog;
    }
}
