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
    private EventRegistrationPolicy? _policy;
    private TicketCatalog? _catalog;
    private TicketedEventLifecycleGuard? _guard;

    public TicketedEventId EventId { get; } = TicketedEventId.New();
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
        f._policy = f.MakeOpenPolicy();
        f._catalog = f.MakeCatalog(("general-admission", "General Admission", max, used));
        return f;
    }

    public static SelfRegisterAttendeeFixture CapacityFull()
    {
        var f = new SelfRegisterAttendeeFixture();
        f._policy = f.MakeOpenPolicy();
        f._catalog = f.MakeCatalog(("workshop", "Workshop", 20, 20));
        return f;
    }

    public static SelfRegisterAttendeeFixture NoCapacitySet()
    {
        var f = new SelfRegisterAttendeeFixture();
        f._policy = f.MakeOpenPolicy();
        f._catalog = f.MakeCatalog(("speaker-pass", "Speaker Pass", null, 0));
        return f;
    }

    public static SelfRegisterAttendeeFixture WindowNotYetOpen()
    {
        var f = new SelfRegisterAttendeeFixture();
        var opensAt = DateTimeOffset.UtcNow.AddDays(1);
        var closesAt = DateTimeOffset.UtcNow.AddDays(30);
        f._policy = EventRegistrationPolicy.Create(f.EventId);
        f._policy.SetWindow(opensAt, closesAt);
        f._catalog = f.MakeCatalog(("general-admission", "General Admission", 100, 0));
        return f;
    }

    public static SelfRegisterAttendeeFixture WindowClosed()
    {
        var f = new SelfRegisterAttendeeFixture();
        var opensAt = DateTimeOffset.UtcNow.AddDays(-30);
        var closesAt = DateTimeOffset.UtcNow.AddDays(-1);
        f._policy = EventRegistrationPolicy.Create(f.EventId);
        f._policy.SetWindow(opensAt, closesAt);
        f._catalog = f.MakeCatalog(("general-admission", "General Admission", 100, 0));
        return f;
    }

    public static SelfRegisterAttendeeFixture NoWindowConfigured()
    {
        var f = new SelfRegisterAttendeeFixture();
        f._policy = EventRegistrationPolicy.Create(f.EventId); // no window
        f._catalog = f.MakeCatalog(("general-admission", "General Admission", 100, 0));
        return f;
    }

    public static SelfRegisterAttendeeFixture WithDomainRestriction(string allowedDomain = "@acme.com")
    {
        var f = new SelfRegisterAttendeeFixture();
        f._policy = f.MakeOpenPolicy();
        f._policy.SetDomainRestriction(allowedDomain);
        f._catalog = f.MakeCatalog(("general-admission", "General Admission", 100, 0));
        return f;
    }

    public static SelfRegisterAttendeeFixture WithMultipleTicketTypes()
    {
        var f = new SelfRegisterAttendeeFixture();
        f._policy = f.MakeOpenPolicy();
        f._catalog = f.MakeCatalog(
            ("general-admission", "General Admission", 100, 0),
            ("workshop-a", "Workshop A", 20, 0));
        return f;
    }

    public static SelfRegisterAttendeeFixture WithCancelledTicketType()
    {
        var f = new SelfRegisterAttendeeFixture();
        f._policy = f.MakeOpenPolicy();
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
        f._policy = f.MakeOpenPolicy();
        var catalog = TicketCatalog.Create(f.EventId);
        catalog.AddTicketType(Slug.From("workshop-a"), DisplayName.From("Workshop A"),
            [new TimeSlot(Slug.From("morning"))], 20);
        catalog.AddTicketType(Slug.From("workshop-b"), DisplayName.From("Workshop B"),
            [new TimeSlot(Slug.From("morning"))], 20);
        f._catalog = catalog;
        return f;
    }

    public static SelfRegisterAttendeeFixture EventNotActive()
    {
        var f = new SelfRegisterAttendeeFixture();
        f._policy = EventRegistrationPolicy.Create(f.EventId);
        f._policy.SetWindow(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(30));
        f._guard = TicketedEventLifecycleGuard.Create(f.EventId);
        f._guard.SetCancelled();
        f._catalog = f.MakeCatalog(("general-admission", "General Admission", 100, 0));
        return f;
    }

    public static SelfRegisterAttendeeFixture WithExistingRegistration()
    {
        var f = OpenWindowWithCapacity(max: 100, used: 50);
        f._seedExistingRegistration = true;
        return f;
    }

    // ── Setup ────────────────────────────────────────────────────────────────

    public async ValueTask SetupAsync(IntegrationTestEnvironment environment)
    {
        if (_policy is not null || _catalog is not null || _guard is not null)
        {
            await environment.Database.SeedAsync(dbContext =>
            {
                if (_policy is not null)
                    dbContext.EventRegistrationPolicies.Add(_policy);
                if (_catalog is not null)
                    dbContext.TicketCatalogs.Add(_catalog);
                if (_guard is not null)
                    dbContext.TicketedEventLifecycleGuards.Add(_guard);
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

    private EventRegistrationPolicy MakeOpenPolicy()
    {
        var policy = EventRegistrationPolicy.Create(EventId);
        policy.SetWindow(
            DateTimeOffset.UtcNow.AddDays(-1),
            DateTimeOffset.UtcNow.AddDays(30));
        return policy;
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
