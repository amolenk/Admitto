using Amolenk.Admitto.Core.Registrations.Contracts.ValueObjects;
using Amolenk.Admitto.Core.Registrations.Domain.Entities;
using Amolenk.Admitto.Core.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Core.IntegrationTests.Registrations.Application.UseCases.Registrations.UpdatePartnerRegistration;

internal sealed class UpdatePartnerRegistrationFixture
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

    private UpdatePartnerRegistrationFixture() { }

    public static UpdatePartnerRegistrationFixture WithCapacity(
        int earlyBirdMax = 100,
        int earlyBirdUsed = 50,
        int workshopMax = 20,
        int workshopUsed = 10)
    {
        var f = new UpdatePartnerRegistrationFixture();
        f._ticketedEvent = f.MakeActiveEventWithSchema();
        f._catalog = f.MakeCatalog(
            ("early-bird", "Early Bird", earlyBirdMax, earlyBirdUsed, true),
            ("workshop", "Workshop", workshopMax, workshopUsed, true));
        return f;
    }

    public static UpdatePartnerRegistrationFixture WithSoldOutWorkshop()
    {
        var f = new UpdatePartnerRegistrationFixture();
        f._ticketedEvent = f.MakeActiveEventWithSchema();
        f._catalog = f.MakeCatalog(
            ("early-bird", "Early Bird", 100, 50, true),
            ("workshop", "Workshop", 1, 1, true));
        return f;
    }

    public static UpdatePartnerRegistrationFixture WithSelfServiceDisabledWorkshop()
    {
        var f = new UpdatePartnerRegistrationFixture();
        f._ticketedEvent = f.MakeActiveEventWithSchema();
        f._catalog = f.MakeCatalog(
            ("early-bird", "Early Bird", 100, 50, true),
            ("workshop", "Workshop", 20, 10, false));
        return f;
    }

    public static UpdatePartnerRegistrationFixture WithCancelledRegistration()
    {
        var f = new UpdatePartnerRegistrationFixture { _preCancel = true };
        f._ticketedEvent = f.MakeActiveEventWithSchema();
        f._catalog = f.MakeCatalog(("early-bird", "Early Bird", 100, 50, true));
        return f;
    }

    public static UpdatePartnerRegistrationFixture WithWaitlistCoupon(bool overlappingTickets = false)
    {
        var f = new UpdatePartnerRegistrationFixture();
        f._ticketedEvent = f.MakeActiveEventWithSchema();

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
                [new TicketTypeSnapshot(earlyBirdId, TicketTypeName.From("Early Bird"), [])],
                AdditionalDetails.From(new Dictionary<string, string> { ["dietary"] = "old" }));
            RegistrationId = registration.Id;
            if (_preCancel) registration.Cancel(CancellationReason.AttendeeRequest);
            registration.ClearDomainEvents();
            dbContext.Registrations.Add(registration);
        });
    }

    private TicketedEvent MakeActiveEventWithSchema()
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
        ticketedEvent.UpdateAdditionalDetailSchema(
        [
            AdditionalDetailField.Create("dietary", "Dietary", 200),
            AdditionalDetailField.Create("tshirt", "T-shirt", 5)
        ]);
        ticketedEvent.ClearDomainEvents();
        return ticketedEvent;
    }

    private TicketCatalog MakeCatalog(params (string slug, string name, int max, int used, bool selfServiceEnabled)[] ticketTypes)
    {
        var catalog = TicketCatalog.Create(EventId, TeamId);
        foreach (var (slug, name, max, used, selfServiceEnabled) in ticketTypes)
        {
            var id = TicketTypeId.New();
            _ticketTypeIdsBySlug[slug] = id;
            catalog.AddTicketType(id, TicketTypeName.From(name), [], max, selfServiceEnabled);
            for (var i = 0; i < used; i++) catalog.Claim([id], enforce: false);
        }
        catalog.ClearDomainEvents();
        return catalog;
    }
}
