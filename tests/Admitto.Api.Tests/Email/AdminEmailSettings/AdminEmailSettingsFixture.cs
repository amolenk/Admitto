using Amolenk.Admitto.Api.Tests.Infrastructure.Hosting;
using Amolenk.Admitto.Module.Email.Domain.Tests.Builders;
using Amolenk.Admitto.Module.Registrations.Domain.Entities;
using Amolenk.Admitto.Module.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;
using TeamBuilder = Amolenk.Admitto.Module.Organization.Tests.Application.Builders.TeamBuilder;

namespace Amolenk.Admitto.Api.Tests.Email.AdminEmailSettings;

internal sealed class AdminEmailSettingsFixture
{
    public const string TeamSlug = "acme-settings";
    public const string EventSlug = "settingsconf";

    public static string TeamSettingsRoute => $"/admin/teams/{TeamSlug}/email-settings";
    public static string EventSettingsRoute => $"/admin/teams/{TeamSlug}/events/{EventSlug}/email-settings";
    public static string TeamSettingsTestRoute => $"{TeamSettingsRoute}/test";
    public static string EventSettingsTestRoute => $"{EventSettingsRoute}/test";

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
            .WithSmtpHost(environment.MailDevSmtpEndpoint.Host)
            .WithSmtpPort(environment.MailDevSmtpEndpoint.Port)
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
            .ForEvent(eventId)
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
            .WithSmtpHost(environment.MailDevSmtpEndpoint.Host)
            .WithSmtpPort(environment.MailDevSmtpEndpoint.Port)
            .WithFromAddress("team@example.com")
            .Build();

        var eventSettings = new EventEmailSettingsBuilder()
            .ForEvent(eventId)
            .WithSmtpHost(environment.MailDevSmtpEndpoint.Host)
            .WithSmtpPort(environment.MailDevSmtpEndpoint.Port)
            .WithFromAddress("event@example.com")
            .Build();

        await environment.EmailDatabase.SeedAsync(db =>
        {
            db.EmailSettings.Add(teamSettings);
            db.EmailSettings.Add(eventSettings);
        });

        return (teamSettings.Version, eventSettings.Version);
    }

    private async ValueTask<(global::Amolenk.Admitto.Module.Organization.Domain.Entities.Team Team, TicketedEventId EventId)> SeedTeamAndEventAsync(
        EndToEndTestEnvironment environment)
    {
        var team = new TeamBuilder()
            .WithSlug(TeamSlug)
            .Build();

        var eventId = TicketedEventId.New();

        var ticketedEvent = TicketedEvent.Create(
            eventId,
            team.Id,
            Slug.From(TeamSlug),
            Slug.From(EventSlug),
            DisplayName.From("Settings Conf"),
            AbsoluteUrl.From("https://example.com"),
            AbsoluteUrl.From("https://tickets.example.com"),
            DateTimeOffset.UtcNow.AddDays(60),
            DateTimeOffset.UtcNow.AddDays(61),
                TimeZoneId.From("UTC"));

        var catalog = TicketCatalog.Create(eventId);
        catalog.AddTicketType(Slug.From("general"), DisplayName.From("General"), [], 100);

        await environment.OrganizationDatabase.SeedAsync(db => db.Teams.Add(team));
        await environment.RegistrationsDatabase.SeedAsync(db =>
        {
            db.TicketedEvents.Add(ticketedEvent);
            db.TicketCatalogs.Add(catalog);
        });

        return (team, eventId);
    }
}
