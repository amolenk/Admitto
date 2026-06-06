using Amolenk.Admitto.Api.Tests.Infrastructure.Hosting;
using Amolenk.Admitto.Core.Badges.Domain.Entities;
using Amolenk.Admitto.Core.Badges.Domain.ValueObjects;
using Amolenk.Admitto.Core.Registrations.Domain.Entities;
using Amolenk.Admitto.Core.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;
using TeamBuilder = Amolenk.Admitto.Testing.Builders.Organization.Application.TeamBuilder;

namespace Amolenk.Admitto.Api.Tests.Badges;

/// <summary>
/// Shared fixture builder for badge integration tests. Seeds a team, a ticketed event
/// with two ticket types, and a BadgesEvent. Badge types and instances are optionally
/// pre-seeded via the Add* methods before calling SetupAsync.
/// </summary>
internal sealed class BadgesApiFixture
{
    public static readonly Guid TicketTypeAId = new("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    public static readonly Guid TicketTypeBId = new("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

    public Guid TeamId { get; private set; }
    public Guid EventId { get; private set; }

    public string BadgeTypesRoute => $"/admin/teams/{TeamId}/events/{EventId}/badge-types";
    public string BadgeTypeRoute(Guid badgeTypeId) => $"{BadgeTypesRoute}/{badgeTypeId}";
    public string BadgeInstancesRoute(Guid badgeTypeId) => $"{BadgeTypesRoute}/{badgeTypeId}/instances";
    public string BadgeInstanceRoute(Guid badgeTypeId, Guid instanceId) => $"{BadgeInstancesRoute(badgeTypeId)}/{instanceId}";
    public string ExportRoute(Guid badgeTypeId) => $"{BadgeTypesRoute}/{badgeTypeId}/export";

    private readonly bool _archived;
    private readonly List<BadgeTypeSeed> _badgeTypeSeeds = [];
    private readonly List<BadgeInstanceSeed> _badgeInstanceSeeds = [];
    private readonly List<RegistrationSeed> _registrationSeeds = [];
    private readonly Dictionary<BadgeTypeId, uint> _badgeTypeVersions = [];
    private readonly Dictionary<BadgeInstanceId, uint> _badgeInstanceVersions = [];

    private BadgesApiFixture(bool archived) => _archived = archived;

    public static BadgesApiFixture Active() => new(archived: false);

    public static BadgesApiFixture Archived() => new(archived: true);

    public BadgeTypeId AddStandaloneBadgeType(string name)
    {
        var id = BadgeTypeId.New();
        _badgeTypeSeeds.Add(new BadgeTypeSeed(id, name, BadgeKind.Standalone, []));
        return id;
    }

    public BadgeTypeId AddTicketBasedBadgeType(string name, IReadOnlyList<Guid>? ticketTypeIds = null)
    {
        var ids = ticketTypeIds ?? [TicketTypeAId];
        var id = BadgeTypeId.New();
        _badgeTypeSeeds.Add(new BadgeTypeSeed(id, name, BadgeKind.TicketBased, ids));
        return id;
    }

    public BadgeInstanceId AddBadgeInstance(BadgeTypeId badgeTypeId, string displayName, string notes = "")
    {
        var id = BadgeInstanceId.New();
        _badgeInstanceSeeds.Add(new BadgeInstanceSeed(id, badgeTypeId, displayName, notes));
        return id;
    }

    public void AddRegistration(string email, string firstName, string lastName, bool cancelled = false)
    {
        _registrationSeeds.Add(new RegistrationSeed(email, firstName, lastName, cancelled));
    }

    public uint BadgeTypeVersion(BadgeTypeId id) => _badgeTypeVersions[id];

    public uint BadgeInstanceVersion(BadgeInstanceId id) => _badgeInstanceVersions[id];

    public async ValueTask SetupAsync(EndToEndTestEnvironment environment)
    {
        var team = new TeamBuilder().Build();
        TeamId = team.Id.Value;

        var eventId = TicketedEventId.New();
        EventId = eventId.Value;

        var ticketedEvent = TicketedEvent.Create(
            CreationRequestId.From(Guid.NewGuid()),
            eventId,
            team.Id,
            EventName.From("BadgesConf"),
            AbsoluteUrl.From("https://example.com"),
            AbsoluteUrl.From("https://tickets.example.com"),
            DateTimeOffset.UtcNow.AddDays(60),
            DateTimeOffset.UtcNow.AddDays(61),
            TimeZoneId.From("UTC"));

        var catalog = TicketCatalog.Create(eventId, team.Id);
        catalog.AddTicketType(TicketTypeId.From(TicketTypeAId), TicketTypeName.From("General Admission"), [], 100);
        catalog.AddTicketType(TicketTypeId.From(TicketTypeBId), TicketTypeName.From("VIP"), [], 50);

        var registrations = _registrationSeeds.Select(seed =>
        {
            var snapshot = new TicketTypeSnapshot(
                TicketTypeId.From(TicketTypeAId),
                TicketTypeName.From("General Admission"),
                []);
            var reg = Registration.Create(
                team.Id,
                eventId,
                EmailAddress.From(seed.Email),
                FirstName.From(seed.FirstName),
                LastName.From(seed.LastName),
                [snapshot]);
            if (seed.Cancelled)
                reg.Cancel(CancellationReason.AttendeeRequest);
            return reg;
        }).ToList();

        var badgesEvent = BadgeEvent.Create(eventId, team.Id);
        if (_archived)
            badgesEvent.MarkArchived();

        var badgeTypeEntities = _badgeTypeSeeds.Select(seed =>
        {
            var ticketTypeVoIds = seed.TicketTypeIds
                .Select(t => TicketTypeId.From(t))
                .ToList();
            return BadgeType.Create(seed.Id, eventId, BadgeTypeName.From(seed.Name), seed.Kind, ticketTypeVoIds);
        }).ToList();

        var badgeInstanceEntities = _badgeInstanceSeeds.Select(seed =>
            BadgeInstance.Create(
                seed.Id,
                seed.BadgeTypeId,
                BadgeInstanceDisplayName.From(seed.DisplayName),
                BadgeInstanceNotes.From(seed.Notes))
        ).ToList();

        await environment.OrganizationDatabase.SeedAsync(db => db.Teams.Add(team));
        await environment.RegistrationsDatabase.SeedAsync(db =>
        {
            db.TicketedEvents.Add(ticketedEvent);
            db.TicketCatalogs.Add(catalog);
            foreach (var reg in registrations)
                db.Registrations.Add(reg);
        });
        await environment.BadgesDatabase.SeedAsync(db =>
        {
            db.BadgeEvents.Add(badgesEvent);
            foreach (var entity in badgeTypeEntities)
                db.BadgeTypes.Add(entity);
            foreach (var entity in badgeInstanceEntities)
                db.BadgeInstances.Add(entity);
        });

        foreach (var (seed, entity) in _badgeTypeSeeds.Zip(badgeTypeEntities))
            _badgeTypeVersions[seed.Id] = entity.Version;
        foreach (var (seed, entity) in _badgeInstanceSeeds.Zip(badgeInstanceEntities))
            _badgeInstanceVersions[seed.Id] = entity.Version;
    }

    private sealed record BadgeTypeSeed(BadgeTypeId Id, string Name, BadgeKind Kind, IReadOnlyList<Guid> TicketTypeIds);
    private sealed record BadgeInstanceSeed(BadgeInstanceId Id, BadgeTypeId BadgeTypeId, string DisplayName, string Notes);
    private sealed record RegistrationSeed(string Email, string FirstName, string LastName, bool Cancelled);
}
