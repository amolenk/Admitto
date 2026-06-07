using Amolenk.Admitto.Api.Tests.Infrastructure.Hosting;
using Amolenk.Admitto.Testing.Builders.Email.Domain;
using Amolenk.Admitto.Core.Registrations.Domain.Entities;
using Amolenk.Admitto.Core.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;
using TeamBuilder = Amolenk.Admitto.Testing.Builders.Organization.Application.TeamBuilder;

namespace Amolenk.Admitto.Api.Tests.Email.AdminEmailSettings;

internal sealed class AdminEmailSettingsFixture
{
    public Guid TeamId { get; private set; }
    public Guid EventId { get; private set; }

    public string TeamSettingsRoute => $"/admin/teams/{TeamId}/email-settings";
    public string EventSettingsRoute => $"/admin/teams/{TeamId}/events/{EventId}/email-settings";
    public string TeamSettingsTestRoute => $"{TeamSettingsRoute}/test";
    public string EventSettingsTestRoute => $"{EventSettingsRoute}/test";

    private AdminEmailSettingsFixture() { }

    /// <summary>No pre-seeded settings — used for create (PUT without Version) tests.</summary>
    public static AdminEmailSettingsFixture EmptySettings() => new();

    /// <summary>Pre-seeded settings at team scope — used for GET, update, delete, and stale-version tests.</summary>
    public static AdminEmailSettingsFixture WithTeamSettings() => new();

    /// <summary>Pre-seeded settings at both scopes — used for event-scope GET/update/delete tests.</summary>
    public static AdminEmailSettingsFixture WithBothSettings() => new();

    public async ValueTask<uint> SetupEmptyAsync(EndToEndTestEnvironment environment)
    {
        await SeedTeamAndEventAsync(environment);
        return 0;
    }

    public async ValueTask<uint> SetupTeamSettingsAsync(EndToEndTestEnvironment environment)
    {
        var (team, _) = await SeedTeamAndEventAsync(environment);

        var settings = new EventEmailSettingsBuilder()
            .ForTeam(team.Id)
            .WithFromAddress("team@example.com")
            .Build();

        await environment.EmailDatabase.SeedAsync(db => db.EmailSettings.Add(settings));
        return settings.Version;
    }

    public async ValueTask<uint> SetupTeamSmtpSettingsAsync(EndToEndTestEnvironment environment)
    {
        var (team, _) = await SeedTeamAndEventAsync(environment);

        var settings = new EventEmailSettingsBuilder()
            .ForTeam(team.Id)
            .WithSmtpHost(environment.Email.SmtpEndpoint.Host)
            .WithSmtpPort(environment.Email.SmtpEndpoint.Port)
            .WithFromAddress("team@example.com")
            .Build();

        await environment.EmailDatabase.SeedAsync(db => db.EmailSettings.Add(settings));
        return settings.Version;
    }

    public async ValueTask<(uint TeamVersion, uint EventVersion)> SetupBothSettingsAsync(EndToEndTestEnvironment environment)
    {
        var (team, eventId) = await SeedTeamAndEventAsync(environment);

        var teamSettings = new EventEmailSettingsBuilder()
            .ForTeam(team.Id)
            .WithFromAddress("team@example.com")
            .Build();

        var eventSettings = new EventEmailSettingsBuilder()
            .ForTeamAndEvent(team.Id, eventId)
            .WithFromAddress("event@example.com")
            .Build();

        await environment.EmailDatabase.SeedAsync(db =>
        {
            db.EmailSettings.Add(teamSettings);
            db.EmailSettings.Add(eventSettings);
        });

        return (teamSettings.Version, eventSettings.Version);
    }

    public async ValueTask<(uint TeamVersion, uint EventVersion)> SetupBothSmtpSettingsAsync(EndToEndTestEnvironment environment)
    {
        var (team, eventId) = await SeedTeamAndEventAsync(environment);

        var teamSettings = new EventEmailSettingsBuilder()
            .ForTeam(team.Id)
            .WithSmtpHost(environment.Email.SmtpEndpoint.Host)
            .WithSmtpPort(environment.Email.SmtpEndpoint.Port)
            .WithFromAddress("team@example.com")
            .Build();

        var eventSettings = new EventEmailSettingsBuilder()
            .ForTeamAndEvent(team.Id, eventId)
            .WithSmtpHost(environment.Email.SmtpEndpoint.Host)
            .WithSmtpPort(environment.Email.SmtpEndpoint.Port)
            .WithFromAddress("event@example.com")
            .Build();

        await environment.EmailDatabase.SeedAsync(db =>
        {
            db.EmailSettings.Add(teamSettings);
            db.EmailSettings.Add(eventSettings);
        });

        return (teamSettings.Version, eventSettings.Version);
    }

    private async ValueTask<(global::Amolenk.Admitto.Core.Organization.Domain.Entities.Team Team, TicketedEventId EventId)> SeedTeamAndEventAsync(
        EndToEndTestEnvironment environment)
    {
        var team = new TeamBuilder()
            .Build();

        var eventId = TicketedEventId.New();

        TeamId = team.Id.Value;
        EventId = eventId.Value;

        var ticketedEvent = TicketedEvent.Create(
            CreationRequestId.From(Guid.NewGuid()),
            eventId,
            team.Id,
            EventName.From("Settings Conf"),
            AbsoluteUrl.From("https://example.com"),
            AbsoluteUrl.From("https://tickets.example.com"),
            DateTimeOffset.UtcNow.AddDays(60),
            DateTimeOffset.UtcNow.AddDays(61),
                TimeZoneId.From("UTC"));

        var catalog = TicketCatalog.Create(eventId, team.Id);
        catalog.AddTicketType(TicketTypeId.From(new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa")), TicketTypeName.From("General"), [], 100);

        await environment.OrganizationDatabase.SeedAsync(db => db.Teams.Add(team));
        await environment.RegistrationsDatabase.SeedAsync(db =>
        {
            db.TicketedEvents.Add(ticketedEvent);
            db.TicketCatalogs.Add(catalog);
        });

        return (team, eventId);
    }
}
