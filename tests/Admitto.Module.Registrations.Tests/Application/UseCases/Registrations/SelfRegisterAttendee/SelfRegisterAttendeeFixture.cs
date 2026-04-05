using Amolenk.Admitto.Module.Organization.Contracts;
using Amolenk.Admitto.Module.Registrations.Domain.Entities;
using Amolenk.Admitto.Module.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Module.Registrations.Tests.Application.Infrastructure.Hosting;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;
using NSubstitute;

namespace Amolenk.Admitto.Module.Registrations.Tests.Application.UseCases.Registrations.SelfRegisterAttendee;

internal sealed class SelfRegisterAttendeeFixture
{
    private bool _eventNotActive;
    private bool _seedExistingRegistration;
    private EventRegistrationPolicy? _policy;
    private EventCapacity? _eventCapacity;
    private List<TicketTypeDto> _ticketTypes;

    public TicketedEventId EventId { get; } = TicketedEventId.New();
    public string TicketTypeSlug { get; } = "general-admission";
    public string WorkshopSlug { get; } = "workshop-a";
    public IOrganizationFacade OrganizationFacade { get; } = Substitute.For<IOrganizationFacade>();

    private SelfRegisterAttendeeFixture(List<TicketTypeDto>? ticketTypes = null)
    {
        _ticketTypes = ticketTypes ?? [];
    }

    // ── Factory methods ──────────────────────────────────────────────────────

    public static SelfRegisterAttendeeFixture OpenWindowWithCapacity(int max = 100, int used = 50)
    {
        var f = new SelfRegisterAttendeeFixture([
            new TicketTypeDto { Slug = "general-admission", Name = "General Admission", IsCancelled = false }
        ]);
        f._policy = f.MakeOpenPolicy();
        f._eventCapacity = f.MakeCapacity("general-admission", max, used);
        return f;
    }

    public static SelfRegisterAttendeeFixture CapacityFull()
    {
        var f = new SelfRegisterAttendeeFixture([
            new TicketTypeDto { Slug = "workshop", Name = "Workshop", IsCancelled = false }
        ]);
        f._policy = f.MakeOpenPolicy();
        f._eventCapacity = f.MakeCapacity("workshop", 20, 20);
        return f;
    }

    public static SelfRegisterAttendeeFixture NoCapacitySet()
    {
        var f = new SelfRegisterAttendeeFixture([
            new TicketTypeDto { Slug = "speaker-pass", Name = "Speaker Pass", IsCancelled = false }
        ]);
        f._policy = f.MakeOpenPolicy();
        f._eventCapacity = f.MakeCapacity("speaker-pass", null, 0);
        return f;
    }

    public static SelfRegisterAttendeeFixture WindowNotYetOpen()
    {
        var f = new SelfRegisterAttendeeFixture([
            new TicketTypeDto { Slug = "general-admission", Name = "General Admission", IsCancelled = false }
        ]);
        var opensAt = DateTimeOffset.UtcNow.AddDays(1);
        var closesAt = DateTimeOffset.UtcNow.AddDays(30);
        f._policy = EventRegistrationPolicy.Create(f.EventId);
        f._policy.SetWindow(opensAt, closesAt);
        f._eventCapacity = f.MakeCapacity("general-admission", 100, 0);
        return f;
    }

    public static SelfRegisterAttendeeFixture WindowClosed()
    {
        var f = new SelfRegisterAttendeeFixture([
            new TicketTypeDto { Slug = "general-admission", Name = "General Admission", IsCancelled = false }
        ]);
        var opensAt = DateTimeOffset.UtcNow.AddDays(-30);
        var closesAt = DateTimeOffset.UtcNow.AddDays(-1);
        f._policy = EventRegistrationPolicy.Create(f.EventId);
        f._policy.SetWindow(opensAt, closesAt);
        f._eventCapacity = f.MakeCapacity("general-admission", 100, 0);
        return f;
    }

    public static SelfRegisterAttendeeFixture NoWindowConfigured()
    {
        var f = new SelfRegisterAttendeeFixture([
            new TicketTypeDto { Slug = "general-admission", Name = "General Admission", IsCancelled = false }
        ]);
        f._policy = EventRegistrationPolicy.Create(f.EventId); // no window
        f._eventCapacity = f.MakeCapacity("general-admission", 100, 0);
        return f;
    }

    public static SelfRegisterAttendeeFixture WithDomainRestriction(string allowedDomain = "@acme.com")
    {
        var f = new SelfRegisterAttendeeFixture([
            new TicketTypeDto { Slug = "general-admission", Name = "General Admission", IsCancelled = false }
        ]);
        f._policy = f.MakeOpenPolicy();
        f._policy.SetDomainRestriction(allowedDomain);
        f._eventCapacity = f.MakeCapacity("general-admission", 100, 0);
        return f;
    }

    public static SelfRegisterAttendeeFixture WithMultipleTicketTypes()
    {
        var f = new SelfRegisterAttendeeFixture([
            new TicketTypeDto { Slug = "general-admission", Name = "General Admission", IsCancelled = false },
            new TicketTypeDto { Slug = "workshop-a", Name = "Workshop A", IsCancelled = false }
        ]);
        f._policy = f.MakeOpenPolicy();
        var capacity = EventCapacity.Create(f.EventId);
        capacity.SetTicketCapacity("general-admission", 100);
        capacity.SetTicketCapacity("workshop-a", 20);
        f._eventCapacity = capacity;
        return f;
    }

    public static SelfRegisterAttendeeFixture WithCancelledTicketType()
    {
        var f = new SelfRegisterAttendeeFixture([
            new TicketTypeDto { Slug = "general-admission", Name = "General Admission", IsCancelled = false },
            new TicketTypeDto { Slug = "workshop-a", Name = "Workshop A", IsCancelled = true }
        ]);
        f._policy = f.MakeOpenPolicy();
        f._eventCapacity = f.MakeCapacity("general-admission", 100, 0);
        return f;
    }

    public static SelfRegisterAttendeeFixture WithOverlappingTimeSlots()
    {
        var f = new SelfRegisterAttendeeFixture([
            new TicketTypeDto
            {
                Slug = "workshop-a",
                Name = "Workshop A",
                IsCancelled = false,
                TimeSlots = ["morning"]
            },
            new TicketTypeDto
            {
                Slug = "workshop-b",
                Name = "Workshop B",
                IsCancelled = false,
                TimeSlots = ["morning"]
            }
        ]);
        f._policy = f.MakeOpenPolicy();
        var capacity = EventCapacity.Create(f.EventId);
        capacity.SetTicketCapacity("workshop-a", 20);
        capacity.SetTicketCapacity("workshop-b", 20);
        f._eventCapacity = capacity;
        return f;
    }

    public static SelfRegisterAttendeeFixture EventNotActive()
    {
        return new SelfRegisterAttendeeFixture([
            new TicketTypeDto { Slug = "general-admission", Name = "General Admission", IsCancelled = false }
        ]) { _eventNotActive = true };
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
        OrganizationFacade
            .GetTicketTypesAsync(EventId.Value, Arg.Any<CancellationToken>())
            .Returns(_ticketTypes.ToArray());

        OrganizationFacade
            .IsEventActiveAsync(EventId.Value, Arg.Any<CancellationToken>())
            .Returns(!_eventNotActive);

        if (_policy is not null || _eventCapacity is not null)
        {
            await environment.Database.SeedAsync(dbContext =>
            {
                if (_policy is not null)
                    dbContext.EventRegistrationPolicies.Add(_policy);
                if (_eventCapacity is not null)
                    dbContext.EventCapacities.Add(_eventCapacity);
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

    private EventCapacity MakeCapacity(string slug, int? max, int used)
    {
        var capacity = EventCapacity.Create(EventId);
        capacity.SetTicketCapacity(slug, max);
        if (used > 0)
        {
            for (var i = 0; i < used; i++)
                capacity.Claim([slug], enforce: false);
        }

        return capacity;
    }
}
