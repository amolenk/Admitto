using Amolenk.Admitto.Core.Registrations.Domain.Entities;
using Amolenk.Admitto.Core.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;
using Amolenk.Admitto.Core.Registrations.Contracts.ValueObjects;
using Amolenk.Admitto.Testing.Builders.Registrations.Domain;
using Amolenk.Admitto.Core.Organization.Domain.ValueObjects;

namespace Amolenk.Admitto.Core.IntegrationTests.Registrations.Application.UseCases.Registrations.RegisterAttendee;

/// <summary>
/// Consolidated fixture covering scenarios for all three registration modes
/// (self-service, admin-add, coupon).
/// </summary>
internal sealed class RegisterAttendeeFixture
{
    private readonly Dictionary<string, TicketTypeId> _ticketTypeIdsBySlug = new();
    private ExistingRegistrationSeed? _existingRegistration;
    private RegistrationId? _existingRegistrationId;
    private TicketedEvent? _ticketedEvent;
    private TicketCatalog? _catalog;
    private Coupon? _coupon;
    private global::Amolenk.Admitto.Core.Registrations.Domain.Entities.Waitlist? _waitlist;

    public TicketedEventId EventId { get; } = TicketedEventId.New();
    public TeamId TeamId { get; } = TeamId.New();
    public string TicketTypeSlug { get; private set; } = "general-admission";
    public Guid CouponCode { get; private set; }
    public EmailAddress CouponEmail { get; private set; } = EmailAddress.From("speaker@gmail.com");
    public RegistrationId ExistingRegistrationId =>
        _existingRegistrationId
        ?? throw new InvalidOperationException("No existing registration has been seeded.");

    public TicketTypeId GetTicketTypeId(string slug) => _ticketTypeIdsBySlug[slug];
    public TicketTypeId TicketTypeId => GetTicketTypeId(TicketTypeSlug);

    private RegisterAttendeeFixture()
    {
    }

    // ── Generic factories (apply to all modes) ───────────────────────────────

    public static RegisterAttendeeFixture OpenWindowWithCapacity(int max = 100, int used = 50)
    {
        var f = new RegisterAttendeeFixture();
        f._ticketedEvent = f.MakeActiveEventWithOpenWindow();
        f._catalog = f.MakeCatalog(("general-admission", "General Admission", max, used));
        return f;
    }

    public static RegisterAttendeeFixture CapacityFull()
    {
        var f = new RegisterAttendeeFixture { TicketTypeSlug = "workshop" };
        f._ticketedEvent = f.MakeActiveEventWithOpenWindow();
        f._catalog = f.MakeCatalog(("workshop", "Workshop", 20, 20));
        return f;
    }

    public static RegisterAttendeeFixture NoCapacitySet()
    {
        var f = new RegisterAttendeeFixture { TicketTypeSlug = "speaker-pass" };
        f._ticketedEvent = f.MakeActiveEventWithOpenWindow();
        f._catalog = f.MakeCatalog(("speaker-pass", "Speaker Pass", null, 0, false));
        return f;
    }

    public static RegisterAttendeeFixture WithMultipleTicketTypes()
    {
        var f = new RegisterAttendeeFixture();
        f._ticketedEvent = f.MakeActiveEventWithOpenWindow();
        f._catalog = f.MakeCatalog(
            ("general-admission", "General Admission", 100, 0),
            ("workshop-a", "Workshop A", 20, 0));
        return f;
    }

    public static RegisterAttendeeFixture WithOverlappingTimeSlots()
    {
        var f = new RegisterAttendeeFixture();
        f._ticketedEvent = f.MakeActiveEventWithOpenWindow();
        var catalog = TicketCatalog.Create(f.EventId);
        var workshopAId = TicketTypeId.New();
        var workshopBId = TicketTypeId.New();
        f._ticketTypeIdsBySlug["workshop-a"] = workshopAId;
        f._ticketTypeIdsBySlug["workshop-b"] = workshopBId;
        catalog.AddTicketType(workshopAId, TicketTypeName.From("Workshop A"),
            [TimeSlot.From("morning")], 20);
        catalog.AddTicketType(workshopBId, TicketTypeName.From("Workshop B"),
            [TimeSlot.From("morning")], 20);
        f._catalog = catalog;
        return f;
    }

    public static RegisterAttendeeFixture WithExistingRegistration()
    {
        var f = OpenWindowWithCapacity(max: 100, used: 50);
        f.WithActiveExistingRegistration();
        return f;
    }

    public static RegisterAttendeeFixture WindowNotYetOpen()
    {
        var f = new RegisterAttendeeFixture();
        var policy = TicketedEventRegistrationPolicy.Create(
            DateTimeOffset.UtcNow.AddDays(1),
            DateTimeOffset.UtcNow.AddDays(7));
        f._ticketedEvent = f.MakeActiveEvent(policy);
        f._catalog = f.MakeCatalog(("general-admission", "General Admission", 100, 0));
        return f;
    }

    public static RegisterAttendeeFixture WindowClosed()
    {
        var f = new RegisterAttendeeFixture();
        var policy = TicketedEventRegistrationPolicy.Create(
            DateTimeOffset.UtcNow.AddDays(-7),
            DateTimeOffset.UtcNow.AddDays(-1));
        f._ticketedEvent = f.MakeActiveEvent(policy);
        f._catalog = f.MakeCatalog(("general-admission", "General Admission", 100, 0));
        return f;
    }

    public static RegisterAttendeeFixture WithoutRegistrationPolicy()
    {
        var f = new RegisterAttendeeFixture();
        f._ticketedEvent = f.MakeActiveEvent(policy: null);
        f._catalog = f.MakeCatalog(("general-admission", "General Admission", 100, 0));
        return f;
    }

    public static RegisterAttendeeFixture WithEmailDomainRestriction(string allowedDomain)
    {
        var f = new RegisterAttendeeFixture();
        var policy = TicketedEventRegistrationPolicy.Create(
            DateTimeOffset.UtcNow.AddDays(-1),
            DateTimeOffset.UtcNow.AddDays(1),
            allowedDomain);
        f._ticketedEvent = f.MakeActiveEvent(policy);
        f._catalog = f.MakeCatalog(("general-admission", "General Admission", 100, 0));
        return f;
    }

    public static RegisterAttendeeFixture EventArchived()
    {
        var f = new RegisterAttendeeFixture();
        var ev = f.MakeActiveEventWithOpenWindow();
        ev.Archive();
        f._ticketedEvent = ev;
        var catalog = TicketCatalog.Create(f.EventId);
        var generalId = TicketTypeId.New();
        f._ticketTypeIdsBySlug["general-admission"] = generalId;
        catalog.AddTicketType(generalId, TicketTypeName.From("General Admission"), [], 100);
        f._catalog = catalog;
        return f;
    }

    public static RegisterAttendeeFixture EventNotFound()
    {
        return new RegisterAttendeeFixture();
    }

    public static RegisterAttendeeFixture EventWithoutTicketCatalog()
    {
        var f = new RegisterAttendeeFixture();
        f._ticketedEvent = f.MakeActiveEventWithOpenWindow();
        return f;
    }

    public static RegisterAttendeeFixture WithAdditionalDetailSchema(
        params (string key, string name, int maxLength)[] fields)
    {
        var f = new RegisterAttendeeFixture();
        var ev = f.MakeActiveEventWithOpenWindow();
        ev.UpdateAdditionalDetailSchema(
            fields.Select(x => AdditionalDetailField.Create(x.key, x.name, x.maxLength)).ToArray());
        f._ticketedEvent = ev;
        f._catalog = f.MakeCatalog(("general-admission", "General Admission", 100, 0));
        return f;
    }

    public static RegisterAttendeeFixture ConcurrentArchiveDetectedAtClaim()
    {
        var f = new RegisterAttendeeFixture();
        f._ticketedEvent = f.MakeActiveEventWithOpenWindow();
        var catalog = TicketCatalog.Create(f.EventId);
        var generalId = TicketTypeId.New();
        f._ticketTypeIdsBySlug["general-admission"] = generalId;
        catalog.AddTicketType(generalId, TicketTypeName.From("General Admission"), [], 100);
        catalog.MarkEventArchived();
        f._catalog = catalog;
        return f;
    }

    public RegisterAttendeeFixture WithActiveExistingRegistration(
        string email = "alice@example.com",
        string firstName = "Alice",
        string lastName = "Doe",
        IReadOnlyList<TicketTypeSnapshot>? tickets = null,
        IReadOnlyDictionary<string, string>? additionalDetails = null)
    {
        _existingRegistration = new ExistingRegistrationSeed(
            EmailAddress.From(email),
            FirstName.From(firstName),
            LastName.From(lastName),
            tickets ?? [new TicketTypeSnapshot(TicketTypeId, TicketTypeName.From(TicketTypeSlug), [])],
            AdditionalDetails.From(additionalDetails),
            IsCancelled: false,
            CancellationReason: CancellationReason.AttendeeRequest,
            ReconfirmedAt: null);
        return this;
    }

    public RegisterAttendeeFixture WithCancelledExistingRegistration(
        string email = "alice@example.com",
        string firstName = "Previous",
        string lastName = "Attendee",
        IReadOnlyList<TicketTypeSnapshot>? tickets = null,
        IReadOnlyDictionary<string, string>? additionalDetails = null,
        CancellationReason cancellationReason = CancellationReason.AttendeeRequest,
        bool hasReconfirmed = true,
        DateTimeOffset? reconfirmedAt = null)
    {
        _existingRegistration = new ExistingRegistrationSeed(
            EmailAddress.From(email),
            FirstName.From(firstName),
            LastName.From(lastName),
            tickets ?? [new TicketTypeSnapshot(TicketTypeId.New(), TicketTypeName.From("Previous Ticket"), [])],
            AdditionalDetails.From(additionalDetails),
            IsCancelled: true,
            CancellationReason: cancellationReason,
            ReconfirmedAt: hasReconfirmed ? reconfirmedAt ?? DateTimeOffset.UtcNow.AddHours(-1) : null);
        return this;
    }

    public RegisterAttendeeFixture ConfigureAdditionalDetailSchema(
        params (string key, string name, int maxLength)[] fields)
    {
        if (_ticketedEvent is null)
            throw new InvalidOperationException("A ticketed event must exist before configuring its schema.");

        _ticketedEvent.UpdateAdditionalDetailSchema(
            fields.Select(x => AdditionalDetailField.Create(x.key, x.name, x.maxLength)).ToArray());
        return this;
    }

    // ── Coupon-specific factories ────────────────────────────────────────────

    public static RegisterAttendeeFixture CouponHappyFlow()
    {
        var f = new RegisterAttendeeFixture { TicketTypeSlug = "speaker-pass" };
        f._catalog = f.MakeCatalog(("speaker-pass", "Speaker Pass", 5, 5));
        f._coupon = f.BuildCoupon(bypassWindow: false);
        f._ticketedEvent = f.MakeActiveEventWithOpenWindow();
        return f;
    }

    public static RegisterAttendeeFixture CouponExpired()
    {
        var f = new RegisterAttendeeFixture { TicketTypeSlug = "speaker-pass" };
        f._catalog = f.MakeCatalog(("speaker-pass", "Speaker Pass", 5, 5));
        var ticketTypeId = f.GetTicketTypeId("speaker-pass");
        f._coupon = new CouponBuilder()
            .WithEventId(f.EventId)
            .WithEmail(f.CouponEmail)
            .WithRequestedTicketTypeIds(ticketTypeId)
            .WithAvailableTicketTypes(new TicketTypeInfo(ticketTypeId))
            .WithExpiresAt(DateTimeOffset.UtcNow.AddMinutes(-1))
            .Build();
        f.CouponCode = f._coupon.Code.Value;
        f._ticketedEvent = f.MakeActiveEventWithOpenWindow();
        return f;
    }

    public static RegisterAttendeeFixture CouponRedeemed()
    {
        var f = new RegisterAttendeeFixture { TicketTypeSlug = "speaker-pass" };
        f._catalog = f.MakeCatalog(("speaker-pass", "Speaker Pass", 5, 5));
        f._coupon = f.BuildCoupon();
        f._coupon.Redeem(f._coupon.Email, f._coupon.AllowedTicketTypeIds, DateTimeOffset.UtcNow);
        f.CouponCode = f._coupon.Code.Value;
        f._ticketedEvent = f.MakeActiveEventWithOpenWindow();
        return f;
    }

    public static RegisterAttendeeFixture CouponRevoked()
    {
        var f = new RegisterAttendeeFixture { TicketTypeSlug = "speaker-pass" };
        f._catalog = f.MakeCatalog(("speaker-pass", "Speaker Pass", 5, 5));
        f._coupon = f.BuildCoupon();
        f._coupon.Revoke();
        f.CouponCode = f._coupon.Code.Value;
        f._ticketedEvent = f.MakeActiveEventWithOpenWindow();
        return f;
    }

    public static RegisterAttendeeFixture CouponTicketTypeNotAllowlisted()
    {
        var f = new RegisterAttendeeFixture { TicketTypeSlug = "speaker-pass" };
        f._catalog = f.MakeCatalog(
            ("general-admission", "General Admission", 100, 0),
            ("speaker-pass", "Speaker Pass", 100, 0));
        f._coupon = f.BuildCoupon();
        f.CouponCode = f._coupon.Code.Value;
        f._ticketedEvent = f.MakeActiveEventWithOpenWindow();
        return f;
    }

    public static RegisterAttendeeFixture CouponBypassesNullCapacity()
    {
        var f = new RegisterAttendeeFixture { TicketTypeSlug = "speaker-pass" };
        f._catalog = f.MakeCatalog(("speaker-pass", "Speaker Pass", null, 0));
        f._coupon = f.BuildCoupon(bypassWindow: false);
        f._ticketedEvent = f.MakeActiveEventWithOpenWindow();
        return f;
    }

    public static RegisterAttendeeFixture CouponBypassesClosedWindow()
    {
        var f = new RegisterAttendeeFixture { TicketTypeSlug = "speaker-pass" };
        f._catalog = f.MakeCatalog(("speaker-pass", "Speaker Pass", 100, 0));
        f._coupon = f.BuildCoupon(bypassWindow: true);
        var closedPolicy = TicketedEventRegistrationPolicy.Create(
            DateTimeOffset.UtcNow.AddDays(-7),
            DateTimeOffset.UtcNow.AddDays(-1));
        f._ticketedEvent = f.MakeActiveEvent(closedPolicy);
        return f;
    }

    public static RegisterAttendeeFixture CouponRespectsClosedWindow()
    {
        var f = new RegisterAttendeeFixture { TicketTypeSlug = "speaker-pass" };
        f._catalog = f.MakeCatalog(("speaker-pass", "Speaker Pass", 100, 0));
        f._coupon = f.BuildCoupon(bypassWindow: false);
        var closedPolicy = TicketedEventRegistrationPolicy.Create(
            DateTimeOffset.UtcNow.AddDays(-7),
            DateTimeOffset.UtcNow.AddDays(-1));
        f._ticketedEvent = f.MakeActiveEvent(closedPolicy);
        return f;
    }

    public static RegisterAttendeeFixture CouponBypassesDomainRestriction()
    {
        var f = new RegisterAttendeeFixture { TicketTypeSlug = "speaker-pass" };
        f._catalog = f.MakeCatalog(("speaker-pass", "Speaker Pass", 100, 0));
        f._coupon = f.BuildCoupon(bypassWindow: false);
        var policy = TicketedEventRegistrationPolicy.Create(
            DateTimeOffset.UtcNow.AddDays(-1),
            DateTimeOffset.UtcNow.AddDays(30),
            "@acme.com");
        f._ticketedEvent = f.MakeActiveEvent(policy);
        return f;
    }

    public static RegisterAttendeeFixture CouponEventArchived()
    {
        var f = new RegisterAttendeeFixture { TicketTypeSlug = "speaker-pass" };
        f._catalog = f.MakeCatalog(("speaker-pass", "Speaker Pass", 100, 0));
        f._coupon = f.BuildCoupon(bypassWindow: true);
        var ev = f.MakeActiveEventWithOpenWindow();
        ev.Archive();
        f._ticketedEvent = ev;
        return f;
    }

    public static RegisterAttendeeFixture SelfServiceWithWaitlistMode()
    {
        var f = new RegisterAttendeeFixture { TicketTypeSlug = "general-admission" };
        f._ticketedEvent = f.MakeActiveEventWithOpenWindow();
        f._catalog = f.MakeWaitlistModeCatalog("general-admission", "General Admission", max: 3, preFill: 2);
        return f;
    }

    public static RegisterAttendeeFixture WaitlistCouponHappyFlow()
    {
        var f = new RegisterAttendeeFixture { TicketTypeSlug = "general-admission" };
        f._ticketedEvent = f.MakeActiveEventWithOpenWindow();
        f._catalog = f.MakeWaitlistModeCatalog("general-admission", "General Admission", max: 2, preFill: 1);
        var ticketTypeId = f.GetTicketTypeId("general-admission");

        f._coupon = new CouponBuilder()
            .WithEventId(f.EventId)
            .WithEmail(f.CouponEmail)
            .WithRequestedTicketTypeIds(ticketTypeId)
            .WithAvailableTicketTypes(new TicketTypeInfo(ticketTypeId))
            .WithExpiresAt(DateTimeOffset.UtcNow.AddDays(30))
            .WithSource(CouponSource.Waitlist)
            .Build();
        f.CouponCode = f._coupon.Code.Value;

        var waitlist = global::Amolenk.Admitto.Core.Registrations.Domain.Entities.Waitlist.Create(f.EventId, ticketTypeId, f.TeamId);
        waitlist.TrackIssuedCoupon(f._coupon.Id, DateTimeOffset.UtcNow);
        waitlist.ClearDomainEvents();
        f._waitlist = waitlist;

        return f;
    }

    public static RegisterAttendeeFixture OrganiserCouponWithWaitlistActive()
    {
        var f = new RegisterAttendeeFixture { TicketTypeSlug = "general-admission" };
        f._ticketedEvent = f.MakeActiveEventWithOpenWindow();
        f._catalog = f.MakeWaitlistModeCatalog("general-admission", "General Admission", max: 2, preFill: 1);
        var ticketTypeId = f.GetTicketTypeId("general-admission");

        f._coupon = new CouponBuilder()
            .WithEventId(f.EventId)
            .WithEmail(f.CouponEmail)
            .WithRequestedTicketTypeIds(ticketTypeId)
            .WithAvailableTicketTypes(new TicketTypeInfo(ticketTypeId))
            .WithExpiresAt(DateTimeOffset.UtcNow.AddDays(30))
            .Build(); // Source=Organiser by default
        f.CouponCode = f._coupon.Code.Value;

        var waitlist = global::Amolenk.Admitto.Core.Registrations.Domain.Entities.Waitlist.Create(f.EventId, ticketTypeId, f.TeamId);
        waitlist.ClearDomainEvents();
        f._waitlist = waitlist;

        return f;
    }

    // ── Setup ────────────────────────────────────────────────────────────────

    public async ValueTask SetupAsync(IntegrationTestEnvironment environment)
    {
        if (_ticketedEvent is not null || _catalog is not null || _coupon is not null || _waitlist is not null)
        {
            await environment.RegistrationsDatabase.SeedAsync(dbContext =>
            {
                if (_ticketedEvent is not null)
                    dbContext.TicketedEvents.Add(_ticketedEvent);
                if (_catalog is not null)
                    dbContext.TicketCatalogs.Add(_catalog);
                if (_coupon is not null)
                    dbContext.Coupons.Add(_coupon);
                if (_waitlist is not null)
                    dbContext.Waitlists.Add(_waitlist);
            });
        }

        var existingSeed = _existingRegistration;
        if (existingSeed is not null)
        {
            await environment.RegistrationsDatabase.SeedAsync(dbContext =>
            {
                var existing = Registration.Create(
                    TeamId,
                    EventId,
                    existingSeed.Email,
                    existingSeed.FirstName,
                    existingSeed.LastName,
                    existingSeed.Tickets,
                    existingSeed.AdditionalDetails);

                if (existingSeed.ReconfirmedAt is not null)
                    existing.Reconfirm(existingSeed.ReconfirmedAt.Value);

                if (existingSeed.IsCancelled)
                    existing.Cancel(existingSeed.CancellationReason);

                existing.ClearDomainEvents();
                _existingRegistrationId = existing.Id;
                dbContext.Registrations.Add(existing);
            });
        }
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private Coupon BuildCoupon(bool bypassWindow = false)
    {
        var ticketTypeId = GetTicketTypeId(TicketTypeSlug);
        var coupon = new CouponBuilder()
            .WithEventId(EventId)
            .WithEmail(CouponEmail)
            .WithRequestedTicketTypeIds(ticketTypeId)
            .WithAvailableTicketTypes(new TicketTypeInfo(ticketTypeId))
            .WithExpiresAt(DateTimeOffset.UtcNow.AddDays(30))
            .WithBypassRegistrationWindow(bypassWindow)
            .Build();
        CouponCode = coupon.Code.Value;
        return coupon;
    }

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
            CreationRequestId.From(Guid.NewGuid()),
            EventId,
            TeamId,
            EventName.From("DevConf"),
            AbsoluteUrl.From("https://example.com"),
            AbsoluteUrl.From("https://tickets.example.com"),
            DateTimeOffset.UtcNow.AddDays(60),
            DateTimeOffset.UtcNow.AddDays(61),
                TimeZoneId.From("UTC"));
        if (policy is not null)
            ev.ConfigureRegistrationPolicy(policy);
        return ev;
    }

    private TicketCatalog MakeCatalog(params (string slug, string name, int? max, int used)[] ticketTypes) =>
        MakeCatalog(ticketTypes.Select(t => (t.slug, t.name, t.max, t.used, true)).ToArray());

    private TicketCatalog MakeCatalog(params (string slug, string name, int? max, int used, bool selfServiceEnabled)[] ticketTypes)
    {
        var catalog = TicketCatalog.Create(EventId);
        foreach (var (slug, name, max, used, selfServiceEnabled) in ticketTypes)
        {
            var id = TicketTypeId.New();
            _ticketTypeIdsBySlug[slug] = id;
            catalog.AddTicketType(id, TicketTypeName.From(name), [], max, selfServiceEnabled);
            for (var i = 0; i < used; i++)
                catalog.Claim([id], enforce: false);
        }
        return catalog;
    }

    /// <summary>
    /// Creates a catalog with a single WaitlistEnabled ticket type where WaitlistMode is active.
    /// Uses <paramref name="preFill"/> uncapped claims to set initial used capacity, then one
    /// enforced claim to fill the last slot and trigger WaitlistMode activation.
    /// </summary>
    private TicketCatalog MakeWaitlistModeCatalog(string slug, string name, int max, int preFill)
    {
        var catalog = TicketCatalog.Create(EventId);
        var id = TicketTypeId.New();
        _ticketTypeIdsBySlug[slug] = id;
        catalog.AddTicketType(id, TicketTypeName.From(name), [], max, waitlistEnabled: true);
        for (var i = 0; i < preFill; i++)
            catalog.Claim([id], enforce: false);
        catalog.Claim([id], enforce: true); // fills last slot → activates WaitlistMode
        catalog.ClearDomainEvents();
        return catalog;
    }

    private sealed record ExistingRegistrationSeed(
        EmailAddress Email,
        FirstName FirstName,
        LastName LastName,
        IReadOnlyList<TicketTypeSnapshot> Tickets,
        AdditionalDetails AdditionalDetails,
        bool IsCancelled,
        CancellationReason CancellationReason,
        DateTimeOffset? ReconfirmedAt);
}
