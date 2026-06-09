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
    private Coupon? _coupon;
    private global::Amolenk.Admitto.Core.Registrations.Domain.Entities.Waitlist? _waitlist;
    private bool _preCancel;

    public TicketedEventId EventId { get; } = TicketedEventId.New();
    public TeamId TeamId { get; } = TeamId.New();
    public RegistrationId RegistrationId { get; private set; } = RegistrationId.New();
    public Guid WaitlistCouponCode => _coupon?.Code.Value ?? throw new InvalidOperationException("No coupon has been seeded.");

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

    public static ChangeAttendeeTicketsFixture WithWaitlistCoupon(bool overlappingTickets = false)
    {
        var f = new ChangeAttendeeTicketsFixture();
        f._ticketedEvent = f.MakeActiveEvent();

        var catalog = TicketCatalog.Create(f.EventId, f.TeamId);
        var earlyBirdId = TicketTypeId.New();
        var workshopId = TicketTypeId.New();
        f._ticketTypeIdsBySlug["early-bird"] = earlyBirdId;
        f._ticketTypeIdsBySlug["workshop"] = workshopId;

        var workshopSlot = overlappingTickets ? "morning" : "afternoon";
        catalog.AddTicketType(earlyBirdId, TicketTypeName.From("Early Bird"), [TimeSlot.From("morning")], 100);
        catalog.AddTicketType(workshopId, TicketTypeName.From("Workshop"), [TimeSlot.From(workshopSlot)], 1, waitlistEnabled: true);
        catalog.Claim([earlyBirdId], enforce: false);
        catalog.Claim([workshopId], enforce: true);
        catalog.ClearDomainEvents();
        f._catalog = catalog;

        f._coupon = Coupon.Create(
            f.EventId,
            f.TeamId,
            EmailAddress.From("alice@example.com"),
            [workshopId],
            DateTimeOffset.UtcNow.AddDays(30),
            bypassRegistrationWindow: true,
            [new TicketTypeInfo(workshopId)],
            DateTimeOffset.UtcNow,
            CouponSource.Waitlist);
        f._coupon.ClearDomainEvents();

        var waitlist = global::Amolenk.Admitto.Core.Registrations.Domain.Entities.Waitlist.Create(f.EventId, workshopId, f.TeamId);
        waitlist.TrackIssuedCoupon(f._coupon.Id, DateTimeOffset.UtcNow);
        waitlist.ClearDomainEvents();
        f._waitlist = waitlist;

        return f;
    }

    public async ValueTask SetupAsync(IntegrationTestEnvironment environment)
    {
        await environment.RegistrationsDatabase.SeedAsync(dbContext =>
        {
            if (_ticketedEvent is not null) dbContext.TicketedEvents.Add(_ticketedEvent);
            if (_catalog is not null) dbContext.TicketCatalogs.Add(_catalog);
            if (_coupon is not null) dbContext.Coupons.Add(_coupon);
            if (_waitlist is not null) dbContext.Waitlists.Add(_waitlist);

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

    private TicketedEvent MakeActiveEvent()
    {
        var ticketedEvent = TicketedEvent.Create(
            CreationRequestId.From(Guid.NewGuid()),
            EventId,
            TeamId,
            EventName.From("DevConf"),
            AbsoluteUrl.From("https://example.com"),
            AbsoluteUrl.From("https://tickets.example.com"),
            DateTimeOffset.UtcNow.AddDays(60),
            DateTimeOffset.UtcNow.AddDays(61),
            TimeZoneId.From("UTC"));

        ticketedEvent.ConfigureRegistrationPolicy(TicketedEventRegistrationPolicy.Create(
            DateTimeOffset.UtcNow.AddDays(-1),
            DateTimeOffset.UtcNow.AddDays(30)));
        return ticketedEvent;
    }

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
