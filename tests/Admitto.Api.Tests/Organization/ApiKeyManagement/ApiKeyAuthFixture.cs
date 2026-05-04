using Amolenk.Admitto.Api.Tests.Infrastructure;
using Amolenk.Admitto.Api.Tests.Infrastructure.Hosting;
using Amolenk.Admitto.Module.Organization.Domain.Entities;
using Amolenk.Admitto.Module.Registrations.Domain.Entities;
using Amolenk.Admitto.Module.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;
using TeamBuilder = Amolenk.Admitto.Module.Organization.Tests.Application.Builders.TeamBuilder;

namespace Amolenk.Admitto.Api.Tests.Organization.ApiKeyManagement;

internal sealed class ApiKeyAuthFixture
{
    private readonly bool _seedApiKey;
    private readonly bool _revokeApiKey;
    private readonly bool _seedSecondTeam;
    private readonly bool _seedEvent;

    private ApiKeyAuthFixture(
        bool seedApiKey = false,
        bool revokeApiKey = false,
        bool seedSecondTeam = false,
        bool seedEvent = false)
    {
        _seedApiKey = seedApiKey;
        _revokeApiKey = revokeApiKey;
        _seedSecondTeam = seedSecondTeam;
        _seedEvent = seedEvent;
    }

    public const string TeamSlug = "team-a";
    public const string EventSlug = "devconf";
    public const string OtherTeamSlug = "team-b";
    public const string OtherEventSlug = "otherconf";

    public string ApiKey => ApiKeyTestHelper.TestRawKey;
    public string OtherTeamApiKey => ApiKeyTestHelper.TestRawKey2;

    public Guid ApiKeyId { get; private set; }
    public Guid OtherTeamApiKeyId { get; private set; }

    public static ApiKeyAuthFixture WithTeam() => new();
    public static ApiKeyAuthFixture WithSeededApiKey() => new(seedApiKey: true);
    public static ApiKeyAuthFixture WithRevokedApiKey() => new(seedApiKey: true, revokeApiKey: true);
    public static ApiKeyAuthFixture WithTwoTeams() => new(seedApiKey: true, seedSecondTeam: true);
    public static ApiKeyAuthFixture WithTeamAndEvent() => new(seedApiKey: true, seedEvent: true);
    public static ApiKeyAuthFixture WithTeamAndRevokedApiKey() => new(seedApiKey: true, seedEvent: true, revokeApiKey: true);
    public static ApiKeyAuthFixture WithTwoTeamsAndEvents() => new(seedApiKey: true, seedSecondTeam: true, seedEvent: true);

    public async ValueTask SetupAsync(EndToEndTestEnvironment environment)
    {
        var team = new TeamBuilder().WithSlug(TeamSlug).Build();

        ApiKey? teamAApiKey = null;
        if (_seedApiKey)
        {
            teamAApiKey = ApiKeyTestHelper.CreateApiKeyEntity(team.Id);
            ApiKeyId = teamAApiKey.Id.Value;
            if (_revokeApiKey)
                teamAApiKey.Revoke(DateTimeOffset.UtcNow.AddMinutes(-1));
        }

        Team? otherTeam = null;
        ApiKey? otherApiKey = null;
        if (_seedSecondTeam)
        {
            otherTeam = new TeamBuilder().WithSlug(OtherTeamSlug).Build();
            otherApiKey = ApiKeyTestHelper.CreateApiKeyEntity2(otherTeam.Id);
            OtherTeamApiKeyId = otherApiKey.Id.Value;
        }

        await environment.OrganizationDatabase.SeedAsync(db =>
        {
            db.Teams.Add(team);
            if (teamAApiKey is not null) db.ApiKeys.Add(teamAApiKey);
            if (otherTeam is not null) db.Teams.Add(otherTeam);
            if (otherApiKey is not null) db.ApiKeys.Add(otherApiKey);
        });

        if (_seedEvent)
        {
            await environment.RegistrationsDatabase.SeedAsync(db =>
            {
                var primaryEvent = BuildEvent(team.Id, TeamSlug, EventSlug, "DevConf");
                var primaryCatalog = TicketCatalog.Create(primaryEvent.Id);
                primaryCatalog.AddTicketType(
                    Slug.From("general-admission"), DisplayName.From("General Admission"), [], 100);
                db.TicketedEvents.Add(primaryEvent);
                db.TicketCatalogs.Add(primaryCatalog);

                if (otherTeam is not null)
                {
                    var otherEvent = BuildEvent(otherTeam.Id, OtherTeamSlug, OtherEventSlug, "OtherConf");
                    var otherCatalog = TicketCatalog.Create(otherEvent.Id);
                    otherCatalog.AddTicketType(
                        Slug.From("general-admission"), DisplayName.From("General Admission"), [], 100);
                    db.TicketedEvents.Add(otherEvent);
                    db.TicketCatalogs.Add(otherCatalog);
                }
            });
        }
    }

    private static TicketedEvent BuildEvent(TeamId teamId, string teamSlug, string eventSlug, string displayName)
    {
        var ticketedEvent = TicketedEvent.Create(
            TicketedEventId.New(),
            teamId,
            Slug.From(teamSlug),
            Slug.From(eventSlug),
            DisplayName.From(displayName),
            AbsoluteUrl.From("https://example.com"),
            AbsoluteUrl.From("https://tickets.example.com"),
            DateTimeOffset.UtcNow.AddDays(60),
            DateTimeOffset.UtcNow.AddDays(61),
            TimeZoneId.From("UTC"));
        ticketedEvent.ConfigureRegistrationPolicy(
            TicketedEventRegistrationPolicy.Create(
                DateTimeOffset.UtcNow.AddDays(-1),
                DateTimeOffset.UtcNow.AddDays(30)));
        return ticketedEvent;
    }
}
