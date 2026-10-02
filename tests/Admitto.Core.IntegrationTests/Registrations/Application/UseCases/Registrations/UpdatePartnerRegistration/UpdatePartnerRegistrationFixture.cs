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
    private readonly List<global::Amolenk.Admitto.Core.Registrations.Domain.Entities.Waitlist> _waitlists = [];
    private bool _preCancel;
    private TicketTypeSnapshot? _adminTicket;

    public TicketedEventId EventId { get; } = TicketedEventId.New();
    public TeamId TeamId { get; } = TeamId.New();
    public RegistrationId RegistrationId { get; private set; } = RegistrationId.New();
    public Guid CouponCode => _coupon?.Code.Value ?? throw new InvalidOperationException("No coupon has been seeded.");
    public static EmailAddress AttendeeEmail { get; } = EmailAddress.From("alice@example.com");
    public static EmailAddress OtherQueuedEmail { get; } = EmailAddress.From("bob@example.com");

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

    public static UpdatePartnerRegistrationFixture WithAdminTicket()
    {
        var f = new UpdatePartnerRegistrationFixture();
        f._ticketedEvent = f.MakeActiveEventWithSchema();

        var catalog = TicketCatalog.Create(f.EventId, f.TeamId);
        var vipId = TicketTypeId.New();
        var earlyBirdId = TicketTypeId.New();
        f._ticketTypeIdsBySlug["vip"] = vipId;
        f._ticketTypeIdsBySlug["early-bird"] = earlyBirdId;

        catalog.AddTicketType(vipId, TicketTypeName.From("VIP"), [], publicCapacity: 10);
        catalog.AddTicketType(earlyBirdId, TicketTypeName.From("Early Bird"), [], publicCapacity: 100);

        // The registration's only ticket is an admin ticket.
        var adminTickets = catalog.Claim([vipId], ClaimMode.Admin);
        catalog.ClearDomainEvents();
        f._catalog = catalog;
        f._adminTicket = adminTickets[0];

        return f;
    }

    /// <summary>
    /// Catalog with the registration's existing "early-bird" ticket and a sold-out, waitlist-mode "workshop".
    /// A single-ticket-type waitlist coupon for the workshop was issued to the attendee (their own entry was
    /// removed at issuance); another attendee is still queued behind them.
    /// </summary>
    public static UpdatePartnerRegistrationFixture WithWaitlistCoupon()
    {
        var f = new UpdatePartnerRegistrationFixture();
        f._ticketedEvent = f.MakeActiveEventWithSchema();

        var catalog = TicketCatalog.Create(f.EventId, f.TeamId);
        var earlyBirdId = TicketTypeId.New();
        var workshopId = TicketTypeId.New();
        f._ticketTypeIdsBySlug["early-bird"] = earlyBirdId;
        f._ticketTypeIdsBySlug["workshop"] = workshopId;

        catalog.AddTicketType(earlyBirdId, TicketTypeName.From("Early Bird"), [TimeSlot.From("morning")], 100);
        catalog.AddTicketType(workshopId, TicketTypeName.From("Workshop"), [TimeSlot.From("afternoon")], 1, waitlistEnabled: true);
        catalog.Claim([earlyBirdId], ClaimMode.Public);
        catalog.Claim([workshopId], ClaimMode.Public);
        catalog.ClearDomainEvents();
        f._catalog = catalog;

        var waitlist = global::Amolenk.Admitto.Core.Registrations.Domain.Entities.Waitlist.Create(f.EventId, workshopId, f.TeamId);
        waitlist.AddEntry(AttendeeEmail, DateTimeOffset.UtcNow, catalog, RegistrationId.New());
        waitlist.AddEntry(OtherQueuedEmail, DateTimeOffset.UtcNow, catalog, RegistrationId.New());
        f._coupon = waitlist.IssueNextCoupon(
            f._ticketedEvent, catalog, DateTimeOffset.UtcNow)!;
        f._coupon.ClearDomainEvents();
        waitlist.ClearDomainEvents();
        f._waitlists.Add(waitlist);

        return f;
    }

    /// <summary>
    /// Catalog with the registration's existing "early-bird" ticket and two sold-out, waitlist-mode ticket types,
    /// "workshop" and "masterclass". The attendee is queued on both waitlists (ahead of another attendee on the
    /// workshop one) and holds an organiser-issued coupon covering both ticket types — or, with
    /// <paramref name="coverExistingTicket"/>, covering the already-held early-bird and the workshop.
    /// </summary>
    public static UpdatePartnerRegistrationFixture WithOrganiserMultiTicketTypeCoupon(bool coverExistingTicket = false)
    {
        var f = new UpdatePartnerRegistrationFixture();
        f._ticketedEvent = f.MakeActiveEventWithSchema();

        var catalog = TicketCatalog.Create(f.EventId, f.TeamId);
        var earlyBirdId = TicketTypeId.New();
        var workshopId = TicketTypeId.New();
        var masterclassId = TicketTypeId.New();
        f._ticketTypeIdsBySlug["early-bird"] = earlyBirdId;
        f._ticketTypeIdsBySlug["workshop"] = workshopId;
        f._ticketTypeIdsBySlug["masterclass"] = masterclassId;

        catalog.AddTicketType(earlyBirdId, TicketTypeName.From("Early Bird"), [], 100);
        catalog.AddTicketType(workshopId, TicketTypeName.From("Workshop"), [], 1, waitlistEnabled: true);
        catalog.AddTicketType(masterclassId, TicketTypeName.From("Masterclass"), [], 1, waitlistEnabled: true);
        catalog.Claim([earlyBirdId], ClaimMode.Public);
        catalog.Claim([workshopId], ClaimMode.Public); // fills the last slot -> activates WaitlistMode
        catalog.Claim([masterclassId], ClaimMode.Public); // fills the last slot -> activates WaitlistMode
        catalog.ClearDomainEvents();
        f._catalog = catalog;

        f._coupon = Coupon.Create(
            f.EventId,
            f.TeamId,
            AttendeeEmail,
            coverExistingTicket ? [earlyBirdId, workshopId] : [workshopId, masterclassId],
            DateTimeOffset.UtcNow.AddDays(30),
            bypassRegistrationWindow: false,
            [new TicketTypeInfo(earlyBirdId), new TicketTypeInfo(workshopId), new TicketTypeInfo(masterclassId)],
            DateTimeOffset.UtcNow);
        f._coupon.ClearDomainEvents();

        var workshopWaitlist = global::Amolenk.Admitto.Core.Registrations.Domain.Entities.Waitlist.Create(
            f.EventId, workshopId, f.TeamId);
        workshopWaitlist.AddEntry(AttendeeEmail, DateTimeOffset.UtcNow, catalog, RegistrationId.New());
        workshopWaitlist.AddEntry(OtherQueuedEmail, DateTimeOffset.UtcNow, catalog, RegistrationId.New());
        workshopWaitlist.ClearDomainEvents();
        f._waitlists.Add(workshopWaitlist);

        var masterclassWaitlist = global::Amolenk.Admitto.Core.Registrations.Domain.Entities.Waitlist.Create(
            f.EventId, masterclassId, f.TeamId);
        masterclassWaitlist.AddEntry(AttendeeEmail, DateTimeOffset.UtcNow, catalog, RegistrationId.New());
        masterclassWaitlist.ClearDomainEvents();
        f._waitlists.Add(masterclassWaitlist);

        return f;
    }

    /// <summary>
    /// Catalog with a registerable "early-bird" type (the registration's existing confirmed
    /// ticket) and a waitlist-enabled "workshop" type that is genuinely sold out (WaitlistMode
    /// active). Optionally seeds an active waitlist entry for the registration's email.
    /// </summary>
    public static UpdatePartnerRegistrationFixture WithWaitlistModeWorkshop(bool seedWaitlistEntry = false)
    {
        var f = new UpdatePartnerRegistrationFixture();
        f._ticketedEvent = f.MakeActiveEventWithSchema();

        var catalog = TicketCatalog.Create(f.EventId, f.TeamId);
        var earlyBirdId = TicketTypeId.New();
        var workshopId = TicketTypeId.New();
        f._ticketTypeIdsBySlug["early-bird"] = earlyBirdId;
        f._ticketTypeIdsBySlug["workshop"] = workshopId;

        catalog.AddTicketType(earlyBirdId, TicketTypeName.From("Early Bird"), [], 100);
        catalog.AddTicketType(workshopId, TicketTypeName.From("Workshop"), [], 1, waitlistEnabled: true);
        catalog.Claim([workshopId], ClaimMode.Public); // fills the last slot -> activates WaitlistMode
        catalog.ClearDomainEvents();
        f._catalog = catalog;

        if (seedWaitlistEntry)
        {
            var waitlist = global::Amolenk.Admitto.Core.Registrations.Domain.Entities.Waitlist.Create(
                f.EventId, workshopId, f.TeamId);
            waitlist.AddEntry(AttendeeEmail, DateTimeOffset.UtcNow, catalog, RegistrationId.New());
            waitlist.ClearDomainEvents();
            f._waitlists.Add(waitlist);
        }

        return f;
    }

    /// <summary>
    /// Catalog with a registerable "early-bird" type (the registration's existing confirmed
    /// ticket) and two waitlist-enabled types, "workshop" and "masterclass", that are both
    /// genuinely sold out (WaitlistMode active). The registration's email holds an active
    /// "workshop" waitlist entry.
    /// </summary>
    public static UpdatePartnerRegistrationFixture WithTwoWaitlistModeWorkshops()
    {
        var f = new UpdatePartnerRegistrationFixture();
        f._ticketedEvent = f.MakeActiveEventWithSchema();

        var catalog = TicketCatalog.Create(f.EventId, f.TeamId);
        var earlyBirdId = TicketTypeId.New();
        var workshopId = TicketTypeId.New();
        var masterclassId = TicketTypeId.New();
        f._ticketTypeIdsBySlug["early-bird"] = earlyBirdId;
        f._ticketTypeIdsBySlug["workshop"] = workshopId;
        f._ticketTypeIdsBySlug["masterclass"] = masterclassId;

        catalog.AddTicketType(earlyBirdId, TicketTypeName.From("Early Bird"), [], 100);
        catalog.AddTicketType(workshopId, TicketTypeName.From("Workshop"), [], 1, waitlistEnabled: true);
        catalog.AddTicketType(masterclassId, TicketTypeName.From("Masterclass"), [], 1, waitlistEnabled: true);
        catalog.Claim([workshopId], ClaimMode.Public); // fills the last slot -> activates WaitlistMode
        catalog.Claim([masterclassId], ClaimMode.Public);
        catalog.ClearDomainEvents();
        f._catalog = catalog;

        var waitlist = global::Amolenk.Admitto.Core.Registrations.Domain.Entities.Waitlist.Create(
            f.EventId, workshopId, f.TeamId);
        waitlist.AddEntry(AttendeeEmail, DateTimeOffset.UtcNow, catalog, RegistrationId.New());
        waitlist.ClearDomainEvents();
        f._waitlists.Add(waitlist);

        return f;
    }

    /// <summary>
    /// Catalog with a registerable "early-bird" type (the registration's existing confirmed
    /// ticket) and a waitlist-enabled "workshop" type that currently has available public
    /// capacity (WaitlistMode inactive). Optionally seeds an active waitlist entry for the
    /// registration's email, representing a stale waitlist membership.
    /// </summary>
    public static UpdatePartnerRegistrationFixture WithAvailableWaitlistEnabledWorkshop(bool seedWaitlistEntry = false)
    {
        var f = new UpdatePartnerRegistrationFixture();
        f._ticketedEvent = f.MakeActiveEventWithSchema();

        var catalog = TicketCatalog.Create(f.EventId, f.TeamId);
        var earlyBirdId = TicketTypeId.New();
        var workshopId = TicketTypeId.New();
        f._ticketTypeIdsBySlug["early-bird"] = earlyBirdId;
        f._ticketTypeIdsBySlug["workshop"] = workshopId;

        catalog.AddTicketType(earlyBirdId, TicketTypeName.From("Early Bird"), [], 100);
        catalog.AddTicketType(workshopId, TicketTypeName.From("Workshop"), [], 5, waitlistEnabled: true);
        catalog.ClearDomainEvents();
        f._catalog = catalog;

        if (seedWaitlistEntry)
        {
            var waitlist = global::Amolenk.Admitto.Core.Registrations.Domain.Entities.Waitlist.Create(
                f.EventId, workshopId, f.TeamId);
            waitlist.AddEntry(AttendeeEmail, DateTimeOffset.UtcNow, catalog, RegistrationId.New());
            waitlist.ClearDomainEvents();
            f._waitlists.Add(waitlist);
        }

        return f;
    }

    public async ValueTask SetupAsync(IntegrationTestEnvironment environment)
    {
        await environment.RegistrationsDatabase.SeedAsync(dbContext =>
        {
            if (_ticketedEvent is not null) dbContext.TicketedEvents.Add(_ticketedEvent);
            if (_catalog is not null) dbContext.TicketCatalogs.Add(_catalog);
            if (_coupon is not null) dbContext.Coupons.Add(_coupon);
            dbContext.Waitlists.AddRange(_waitlists);

            var earlyBirdId = _ticketTypeIdsBySlug.TryGetValue("early-bird", out var id) ? id : TicketTypeId.New();
            var initialTickets = _adminTicket is { } adminTicket
                ? [adminTicket]
                : new List<TicketTypeSnapshot> { new(earlyBirdId, TicketTypeName.From("Early Bird"), []) };
            var registration = Registration.Create(
                TeamId,
                EventId,
                AttendeeEmail,
                FirstName.From("Alice"),
                LastName.From("Test"),
                initialTickets,
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

    private TicketCatalog MakeCatalog(params (string slug, string name, int publicCapacity, int used, bool selfServiceEnabled)[] ticketTypes)
    {
        var catalog = TicketCatalog.Create(EventId, TeamId);
        foreach (var (slug, name, publicCapacity, used, selfServiceEnabled) in ticketTypes)
        {
            var id = TicketTypeId.New();
            _ticketTypeIdsBySlug[slug] = id;
            catalog.AddTicketType(id, TicketTypeName.From(name), [], publicCapacity, selfServiceEnabled);
            for (var i = 0; i < used; i++)
                catalog.Claim([id], selfServiceEnabled ? ClaimMode.Public : ClaimMode.Admin);
        }
        catalog.ClearDomainEvents();
        return catalog;
    }
}
