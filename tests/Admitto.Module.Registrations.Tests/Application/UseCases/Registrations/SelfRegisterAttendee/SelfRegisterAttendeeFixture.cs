using Amolenk.Admitto.Module.Organization.Contracts;
using Amolenk.Admitto.Module.Registrations.Domain.Entities;
using Amolenk.Admitto.Module.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Module.Registrations.Tests.Application.Infrastructure.Hosting;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;
using NSubstitute;

namespace Amolenk.Admitto.Module.Registrations.Tests.Application.UseCases.Registrations.SelfRegisterAttendee;

internal sealed class SelfRegisterAttendeeFixture
{
    private bool _seedExistingRegistration;
    private TicketedEvent? _ticketedEvent;
    private TicketCatalog? _catalog;

    public TicketedEventId EventId { get; } = TicketedEventId.New();
    public TeamId TeamId { get; } = TeamId.New();
    public string TicketTypeSlug { get; } = "general-admission";
    public string WorkshopSlug { get; } = "workshop-a";
    public IOrganizationFacade OrganizationFacade { get; } = Substitute.For<IOrganizationFacade>();

    private SelfRegisterAttendeeFixture()
    {
    }

    // ── Factory methods ──────────────────────────────────────────────────────

    public static SelfRegisterAttendeeFixture OpenWindowWithCapacity(int max = 100, int used = 50)
    {
        var f = new SelfRegisterAttendeeFixture();
        f._ticketedEvent = f.MakeActiveEventWithOpenWindow();
        f._catalog = f.MakeCatalog(("general-admission", "General Admission", max, used));
        return f;
    }

    public static SelfRegisterAttendeeFixture CapacityFull()
    {
        var f = new SelfRegisterAttendeeFixture();
        f._ticketedEvent = f.MakeActiveEventWithOpenWindow();
        f._catalog = f.MakeCatalog(("workshop", "Workshop", 20, 20));
        return f;
    }

    public static SelfRegisterAttendeeFixture NoCapacitySet()
    {
        var f = new SelfRegisterAttendeeFixture();
        f._ticketedEvent = f.MakeActiveEventWithOpenWindow();
        f._catalog = f.MakeCatalog(("speaker-pass", "Speaker Pass", null, 0));
        return f;
    }

    public static SelfRegisterAttendeeFixture WithMultipleTicketTypes()
    {
        var f = new SelfRegisterAttendeeFixture();
        f._ticketedEvent = f.MakeActiveEventWithOpenWindow();
        f._catalog = f.MakeCatalog(
            ("general-admission", "General Admission", 100, 0),
            ("workshop-a", "Workshop A", 20, 0));
        return f;
    }

    public static SelfRegisterAttendeeFixture WithCancelledTicketType()
    {
        var f = new SelfRegisterAttendeeFixture();
        f._ticketedEvent = f.MakeActiveEventWithOpenWindow();
        var catalog = TicketCatalog.Create(f.EventId);
        catalog.AddTicketType(Slug.From("general-admission"), DisplayName.From("General Admission"), [], 100);
        catalog.AddTicketType(Slug.From("workshop-a"), DisplayName.From("Workshop A"), [], null);
        catalog.CancelTicketType(Slug.From("workshop-a"));
        f._catalog = catalog;
        return f;
    }

    public static SelfRegisterAttendeeFixture WithOverlappingTimeSlots()
    {
        var f = new SelfRegisterAttendeeFixture();
        f._ticketedEvent = f.MakeActiveEventWithOpenWindow();
        var catalog = TicketCatalog.Create(f.EventId);
        catalog.AddTicketType(Slug.From("workshop-a"), DisplayName.From("Workshop A"),
            [new TimeSlot(Slug.From("morning"))], 20);
        catalog.AddTicketType(Slug.From("workshop-b"), DisplayName.From("Workshop B"),
            [new TimeSlot(Slug.From("morning"))], 20);
        f._catalog = catalog;
        return f;
    }

    public static SelfRegisterAttendeeFixture WithExistingRegistration()
    {
        var f = OpenWindowWithCapacity(max: 100, used: 50);
        f._seedExistingRegistration = true;
        return f;
    }

    public static SelfRegisterAttendeeFixture WindowNotYetOpen()
    {
        var f = new SelfRegisterAttendeeFixture();
        var policy = TicketedEventRegistrationPolicy.Create(
            DateTimeOffset.UtcNow.AddDays(1),
            DateTimeOffset.UtcNow.AddDays(7));
        f._ticketedEvent = f.MakeActiveEvent(policy);
        f._catalog = f.MakeCatalog(("general-admission", "General Admission", 100, 0));
        return f;
    }

    public static SelfRegisterAttendeeFixture WindowClosed()
    {
        var f = new SelfRegisterAttendeeFixture();
        var policy = TicketedEventRegistrationPolicy.Create(
            DateTimeOffset.UtcNow.AddDays(-7),
            DateTimeOffset.UtcNow.AddDays(-1));
        f._ticketedEvent = f.MakeActiveEvent(policy);
        f._catalog = f.MakeCatalog(("general-admission", "General Admission", 100, 0));
        return f;
    }

    public static SelfRegisterAttendeeFixture WithoutRegistrationPolicy()
    {
        var f = new SelfRegisterAttendeeFixture();
        f._ticketedEvent = f.MakeActiveEvent(policy: null);
        f._catalog = f.MakeCatalog(("general-admission", "General Admission", 100, 0));
        return f;
    }

    public static SelfRegisterAttendeeFixture WithEmailDomainRestriction(string allowedDomain)
    {
        var f = new SelfRegisterAttendeeFixture();
        var policy = TicketedEventRegistrationPolicy.Create(
            DateTimeOffset.UtcNow.AddDays(-1),
            DateTimeOffset.UtcNow.AddDays(1),
            allowedDomain);
        f._ticketedEvent = f.MakeActiveEvent(policy);
        f._catalog = f.MakeCatalog(("general-admission", "General Admission", 100, 0));
        return f;
    }

    public static SelfRegisterAttendeeFixture EventCancelled()
    {
        var f = new SelfRegisterAttendeeFixture();
        var ev = f.MakeActiveEventWithOpenWindow();
        ev.Cancel();
        f._ticketedEvent = ev;
        var catalog = TicketCatalog.Create(f.EventId);
        catalog.AddTicketType(Slug.From("general-admission"), DisplayName.From("General Admission"), [], 100);
        // Catalog EventStatus remains Active (the projection is exercised independently);
        // the handler SHALL reject based on TicketedEvent.Status.
        f._catalog = catalog;
        return f;
    }

    public static SelfRegisterAttendeeFixture EventArchived()
    {
        var f = new SelfRegisterAttendeeFixture();
        var ev = f.MakeActiveEventWithOpenWindow();
        ev.Archive();
        f._ticketedEvent = ev;
        var catalog = TicketCatalog.Create(f.EventId);
        catalog.AddTicketType(Slug.From("general-admission"), DisplayName.From("General Admission"), [], 100);
        f._catalog = catalog;
        return f;
    }

    /// <summary>
    /// Concurrent-cancel race: TicketedEvent.Status is Active (the registration handler's
    /// policy check passes) but TicketCatalog.EventStatus has already been flipped to
    /// Cancelled by an in-flight cancel. The atomic claim SHALL reject.
    /// </summary>
    public static SelfRegisterAttendeeFixture ConcurrentCancelDetectedAtClaim()
    {
        var f = new SelfRegisterAttendeeFixture();
        f._ticketedEvent = f.MakeActiveEventWithOpenWindow();
        var catalog = TicketCatalog.Create(f.EventId);
        catalog.AddTicketType(Slug.From("general-admission"), DisplayName.From("General Admission"), [], 100);
        catalog.MarkEventCancelled();
        f._catalog = catalog;
        return f;
    }

    // ── Setup ────────────────────────────────────────────────────────────────

    public async ValueTask SetupAsync(IntegrationTestEnvironment environment)
    {
        if (_ticketedEvent is not null || _catalog is not null)
        {
            await environment.Database.SeedAsync(dbContext =>
            {
                if (_ticketedEvent is not null)
                    dbContext.TicketedEvents.Add(_ticketedEvent);
                if (_catalog is not null)
                    dbContext.TicketCatalogs.Add(_catalog);
            });
        }

        if (_seedExistingRegistration)
        {
            await environment.Database.SeedAsync(dbContext =>
            {
                var existing = Registration.Create(
                    EventId,
                    EmailAddress.From("alice@example.com"),
                    [new TicketTypeSnapshot(TicketTypeSlug, [])]);
                dbContext.Registrations.Add(existing);
            });
        }
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private TicketedEvent MakeActiveEventWithOpenWindow()
    {
        var policy = TicketedEventRegistrationPolicy.Create(
            DateTimeOffset.UtcNow.AddDays(-1),
            DateTimeOffset.UtcNow.AddDays(30));
        return MakeActiveEvent(policy);
    }

    private TicketedEvent MakeActiveEvent(TicketedEventRegistrationPolicy? policy)
    {
        var ev = TicketedEvent.Create(
            EventId,
            TeamId,
            Slug.From("devconf"),
            DisplayName.From("DevConf"),
            AbsoluteUrl.From("https://example.com"),
            AbsoluteUrl.From("https://tickets.example.com"),
            DateTimeOffset.UtcNow.AddDays(60),
            DateTimeOffset.UtcNow.AddDays(61));
        if (policy is not null)
            ev.ConfigureRegistrationPolicy(policy);
        return ev;
    }

    private TicketCatalog MakeCatalog(params (string slug, string name, int? max, int used)[] ticketTypes)
    {
        var catalog = TicketCatalog.Create(EventId);
        foreach (var (slug, name, max, used) in ticketTypes)
        {
            catalog.AddTicketType(Slug.From(slug), DisplayName.From(name), [], max);
            if (used > 0)
            {
                for (var i = 0; i < used; i++)
                    catalog.Claim([slug], enforce: false);
            }
        }
        return catalog;
    }
}
