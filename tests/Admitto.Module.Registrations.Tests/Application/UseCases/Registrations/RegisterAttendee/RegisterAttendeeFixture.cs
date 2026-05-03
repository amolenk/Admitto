using Amolenk.Admitto.Module.Registrations.Domain.Entities;
using Amolenk.Admitto.Module.Registrations.Domain.Tests.Builders;
using Amolenk.Admitto.Module.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Module.Registrations.Tests.Application.Infrastructure.Hosting;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Module.Registrations.Tests.Application.UseCases.Registrations.RegisterAttendee;

/// <summary>
/// Consolidated fixture covering scenarios for all three registration modes
/// (self-service, admin-add, coupon).
/// </summary>
internal sealed class RegisterAttendeeFixture
{
    private ExistingRegistrationSeed? _existingRegistration;
    private RegistrationId? _existingRegistrationId;
    private TicketedEvent? _ticketedEvent;
    private TicketCatalog? _catalog;
    private Coupon? _coupon;

    public TicketedEventId EventId { get; } = TicketedEventId.New();
    public TeamId TeamId { get; } = TeamId.New();
    public string TicketTypeSlug { get; private set; } = "general-admission";
    public string CouponCodeString { get; private set; } = string.Empty;
    public EmailAddress CouponEmail { get; private set; } = EmailAddress.From("speaker@gmail.com");
    public RegistrationId ExistingRegistrationId =>
        _existingRegistrationId
        ?? throw new InvalidOperationException("No existing registration has been seeded.");

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
        f._catalog = f.MakeCatalog(("speaker-pass", "Speaker Pass", null, 0));
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

    public static RegisterAttendeeFixture WithCancelledTicketType()
    {
        var f = new RegisterAttendeeFixture();
        f._ticketedEvent = f.MakeActiveEventWithOpenWindow();
        var catalog = TicketCatalog.Create(f.EventId);
        catalog.AddTicketType(Slug.From("general-admission"), DisplayName.From("General Admission"), [], 100);
        catalog.AddTicketType(Slug.From("workshop-a"), DisplayName.From("Workshop A"), [], null);
        catalog.CancelTicketType(Slug.From("workshop-a"));
        f._catalog = catalog;
        return f;
    }

    public static RegisterAttendeeFixture WithOverlappingTimeSlots()
    {
        var f = new RegisterAttendeeFixture();
        f._ticketedEvent = f.MakeActiveEventWithOpenWindow();
        var catalog = TicketCatalog.Create(f.EventId);
        catalog.AddTicketType(Slug.From("workshop-a"), DisplayName.From("Workshop A"),
            [new TimeSlot(Slug.From("morning"))], 20);
        catalog.AddTicketType(Slug.From("workshop-b"), DisplayName.From("Workshop B"),
            [new TimeSlot(Slug.From("morning"))], 20);
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

    public static RegisterAttendeeFixture EventCancelled()
    {
        var f = new RegisterAttendeeFixture();
        var ev = f.MakeActiveEventWithOpenWindow();
        ev.Cancel();
        f._ticketedEvent = ev;
        var catalog = TicketCatalog.Create(f.EventId);
        catalog.AddTicketType(Slug.From("general-admission"), DisplayName.From("General Admission"), [], 100);
        f._catalog = catalog;
        return f;
    }

    public static RegisterAttendeeFixture EventArchived()
    {
        var f = new RegisterAttendeeFixture();
        var ev = f.MakeActiveEventWithOpenWindow();
        ev.Archive();
        f._ticketedEvent = ev;
        var catalog = TicketCatalog.Create(f.EventId);
        catalog.AddTicketType(Slug.From("general-admission"), DisplayName.From("General Admission"), [], 100);
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

    public static RegisterAttendeeFixture ConcurrentCancelDetectedAtClaim()
    {
        var f = new RegisterAttendeeFixture();
        f._ticketedEvent = f.MakeActiveEventWithOpenWindow();
        var catalog = TicketCatalog.Create(f.EventId);
        catalog.AddTicketType(Slug.From("general-admission"), DisplayName.From("General Admission"), [], 100);
        catalog.MarkEventCancelled();
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
            tickets ?? [new TicketTypeSnapshot(TicketTypeSlug, TicketTypeSlug, [])],
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
            tickets ?? [new TicketTypeSnapshot("previous-ticket", "Previous Ticket", [])],
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
        f._coupon = f.BuildCoupon(bypassWindow: false);
        f._ticketedEvent = f.MakeActiveEventWithOpenWindow();
        f._catalog = f.MakeCatalog(("speaker-pass", "Speaker Pass", 5, 5));
        return f;
    }

    public static RegisterAttendeeFixture CouponExpired()
    {
        var f = new RegisterAttendeeFixture { TicketTypeSlug = "speaker-pass" };
        f._coupon = new CouponBuilder()
            .WithEventId(f.EventId)
            .WithEmail(f.CouponEmail)
            .WithRequestedTicketTypeSlugs(f.TicketTypeSlug)
            .WithAvailableTicketTypes(new TicketTypeInfo(f.TicketTypeSlug, false))
            .WithExpiresAt(DateTimeOffset.UtcNow.AddMinutes(-1))
            .Build();
        f.CouponCodeString = f._coupon.Code.Value.ToString();
        f._ticketedEvent = f.MakeActiveEventWithOpenWindow();
        return f;
    }

    public static RegisterAttendeeFixture CouponRedeemed()
    {
        var f = new RegisterAttendeeFixture { TicketTypeSlug = "speaker-pass" };
        f._coupon = f.BuildCoupon();
        f._coupon.Redeem();
        f.CouponCodeString = f._coupon.Code.Value.ToString();
        f._ticketedEvent = f.MakeActiveEventWithOpenWindow();
        return f;
    }

    public static RegisterAttendeeFixture CouponRevoked()
    {
        var f = new RegisterAttendeeFixture { TicketTypeSlug = "speaker-pass" };
        f._coupon = f.BuildCoupon();
        f._coupon.Revoke();
        f.CouponCodeString = f._coupon.Code.Value.ToString();
        f._ticketedEvent = f.MakeActiveEventWithOpenWindow();
        return f;
    }

    public static RegisterAttendeeFixture CouponTicketTypeNotAllowlisted()
    {
        var f = new RegisterAttendeeFixture { TicketTypeSlug = "speaker-pass" };
        f._coupon = f.BuildCoupon();
        f.CouponCodeString = f._coupon.Code.Value.ToString();
        f._ticketedEvent = f.MakeActiveEventWithOpenWindow();
        f._catalog = f.MakeCatalog(
            ("general-admission", "General Admission", 100, 0),
            (f.TicketTypeSlug, "Speaker Pass", 100, 0));
        return f;
    }

    public static RegisterAttendeeFixture CouponBypassesNullCapacity()
    {
        var f = new RegisterAttendeeFixture { TicketTypeSlug = "speaker-pass" };
        f._coupon = f.BuildCoupon(bypassWindow: false);
        f._ticketedEvent = f.MakeActiveEventWithOpenWindow();
        f._catalog = f.MakeCatalog((f.TicketTypeSlug, "Speaker Pass", null, 0));
        return f;
    }

    public static RegisterAttendeeFixture CouponBypassesClosedWindow()
    {
        var f = new RegisterAttendeeFixture { TicketTypeSlug = "speaker-pass" };
        f._coupon = f.BuildCoupon(bypassWindow: true);
        var closedPolicy = TicketedEventRegistrationPolicy.Create(
            DateTimeOffset.UtcNow.AddDays(-7),
            DateTimeOffset.UtcNow.AddDays(-1));
        f._ticketedEvent = f.MakeActiveEvent(closedPolicy);
        f._catalog = f.MakeCatalog((f.TicketTypeSlug, "Speaker Pass", 100, 0));
        return f;
    }

    public static RegisterAttendeeFixture CouponRespectsClosedWindow()
    {
        var f = new RegisterAttendeeFixture { TicketTypeSlug = "speaker-pass" };
        f._coupon = f.BuildCoupon(bypassWindow: false);
        var closedPolicy = TicketedEventRegistrationPolicy.Create(
            DateTimeOffset.UtcNow.AddDays(-7),
            DateTimeOffset.UtcNow.AddDays(-1));
        f._ticketedEvent = f.MakeActiveEvent(closedPolicy);
        f._catalog = f.MakeCatalog((f.TicketTypeSlug, "Speaker Pass", 100, 0));
        return f;
    }

    public static RegisterAttendeeFixture CouponBypassesDomainRestriction()
    {
        var f = new RegisterAttendeeFixture { TicketTypeSlug = "speaker-pass" };
        f._coupon = f.BuildCoupon(bypassWindow: false);
        var policy = TicketedEventRegistrationPolicy.Create(
            DateTimeOffset.UtcNow.AddDays(-1),
            DateTimeOffset.UtcNow.AddDays(30),
            "@acme.com");
        f._ticketedEvent = f.MakeActiveEvent(policy);
        f._catalog = f.MakeCatalog((f.TicketTypeSlug, "Speaker Pass", 100, 0));
        return f;
    }

    public static RegisterAttendeeFixture CouponEventCancelled()
    {
        var f = new RegisterAttendeeFixture { TicketTypeSlug = "speaker-pass" };
        f._coupon = f.BuildCoupon(bypassWindow: true);
        var ev = f.MakeActiveEventWithOpenWindow();
        ev.Cancel();
        f._ticketedEvent = ev;
        f._catalog = f.MakeCatalog((f.TicketTypeSlug, "Speaker Pass", 100, 0));
        return f;
    }

    // ── Setup ────────────────────────────────────────────────────────────────

    public async ValueTask SetupAsync(IntegrationTestEnvironment environment)
    {
        if (_ticketedEvent is not null || _catalog is not null || _coupon is not null)
        {
            await environment.Database.SeedAsync(dbContext =>
            {
                if (_ticketedEvent is not null)
                    dbContext.TicketedEvents.Add(_ticketedEvent);
                if (_catalog is not null)
                    dbContext.TicketCatalogs.Add(_catalog);
                if (_coupon is not null)
                    dbContext.Coupons.Add(_coupon);
            });
        }

        var existingSeed = _existingRegistration;
        if (existingSeed is not null)
        {
            await environment.Database.SeedAsync(dbContext =>
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
        var coupon = new CouponBuilder()
            .WithEventId(EventId)
            .WithEmail(CouponEmail)
            .WithRequestedTicketTypeSlugs(TicketTypeSlug)
            .WithAvailableTicketTypes(new TicketTypeInfo(TicketTypeSlug, false))
            .WithExpiresAt(DateTimeOffset.UtcNow.AddDays(30))
            .WithBypassRegistrationWindow(bypassWindow)
            .Build();
        CouponCodeString = coupon.Code.Value.ToString();
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
