using Amolenk.Admitto.Api.Tests.Infrastructure.Hosting;
using Amolenk.Admitto.Module.Email.Application.Templating;
using Amolenk.Admitto.Module.Email.Domain.Entities;
using Amolenk.Admitto.Module.Email.Domain.Tests.Builders;
using Amolenk.Admitto.Module.Email.Domain.ValueObjects;
using Amolenk.Admitto.Module.Registrations.Domain.Entities;
using Amolenk.Admitto.Module.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;
using TeamBuilder = Amolenk.Admitto.Module.Organization.Tests.Application.Builders.TeamBuilder;

namespace Amolenk.Admitto.Api.Tests.Email.AdminEmailTemplates;

internal sealed class AdminEmailTemplatesFixture
{
    public Guid TeamId { get; private set; }
    public Guid EventId { get; private set; }
    public const string TemplateName = BuiltInEmailTemplateNames.TicketConfirmation;

    public string TeamTemplatesRoute => $"/admin/teams/{TeamId}/email-templates";
    public string EventTemplatesRoute => $"/admin/teams/{TeamId}/events/{EventId}/email-templates";

    public string TeamTemplateRoute(Guid id) => $"{TeamTemplatesRoute}/{id}";
    public string EventTemplateRoute(Guid id) => $"{EventTemplatesRoute}/{id}";

    private AdminEmailTemplatesFixture() { }

    public static AdminEmailTemplatesFixture EmptyTemplates() => new();
    public static AdminEmailTemplatesFixture WithTeamTemplate() => new();
    public static AdminEmailTemplatesFixture WithBothTemplates() => new();

    public async ValueTask SetupEmptyAsync(EndToEndTestEnvironment environment)
    {
        await SeedTeamAndEventAsync(environment);
    }

    public async ValueTask<(Guid Id, uint Version)> SetupTeamTemplateAsync(EndToEndTestEnvironment environment)
    {
        var (team, _) = await SeedTeamAndEventAsync(environment);

        var template = new EmailTemplateBuilder()
            .ForTeam(team.Id)
            .WithName(TemplateName)
            .WithSubject("Team subject")
            .Build();

        await environment.EmailDatabase.SeedAsync(db => db.EmailTemplates.Add(template));
        return (template.Id.Value, template.Version);
    }

    public async ValueTask<(Guid TeamId, uint TeamVersion, Guid EventId, uint EventVersion)> SetupBothTemplatesAsync(EndToEndTestEnvironment environment)
    {
        var (team, eventId) = await SeedTeamAndEventAsync(environment);

        var teamTemplate = new EmailTemplateBuilder()
            .ForTeam(team.Id)
            .WithName(TemplateName)
            .WithSubject("Team subject")
            .Build();

        var eventTemplate = new EmailTemplateBuilder()
            .ForEvent(eventId)
            .WithName(TemplateName)
            .WithSubject("Event subject")
            .Build();

        await environment.EmailDatabase.SeedAsync(db =>
        {
            db.EmailTemplates.Add(teamTemplate);
            db.EmailTemplates.Add(eventTemplate);
        });

        return (teamTemplate.Id.Value, teamTemplate.Version, eventTemplate.Id.Value, eventTemplate.Version);
    }

    private async ValueTask<(global::Amolenk.Admitto.Module.Organization.Domain.Entities.Team Team, TicketedEventId EventId)> SeedTeamAndEventAsync(
        EndToEndTestEnvironment environment)
    {
        var team = new TeamBuilder()
            .Build();

        var eventId = TicketedEventId.New();

        TeamId = team.Id.Value;
        EventId = eventId.Value;

        var ticketedEvent = TicketedEvent.Create(
            eventId,
            team.Id,
            DisplayName.From("Templates Conf"),
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
