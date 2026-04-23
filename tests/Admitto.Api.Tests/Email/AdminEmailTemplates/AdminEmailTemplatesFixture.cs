using Amolenk.Admitto.Api.Tests.Infrastructure.Hosting;
using Amolenk.Admitto.Module.Email.Domain.Tests.Builders;
using Amolenk.Admitto.Module.Email.Domain.ValueObjects;
using Amolenk.Admitto.Module.Registrations.Domain.Entities;
using Amolenk.Admitto.Module.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;
using TeamBuilder = Amolenk.Admitto.Module.Organization.Tests.Application.Builders.TeamBuilder;

namespace Amolenk.Admitto.Api.Tests.Email.AdminEmailTemplates;

internal sealed class AdminEmailTemplatesFixture
{
    public const string TeamSlug = "acme-templates";
    public const string EventSlug = "templatesconf";
    public const string TemplateType = EmailTemplateType.Ticket;

    public static string TeamTemplateRoute => $"/admin/teams/{TeamSlug}/email-templates/{TemplateType}";
    public static string EventTemplateRoute => $"/admin/teams/{TeamSlug}/events/{EventSlug}/email-templates/{TemplateType}";

    private AdminEmailTemplatesFixture() { }

    public static AdminEmailTemplatesFixture EmptyTemplates() => new();
    public static AdminEmailTemplatesFixture WithTeamTemplate() => new();
    public static AdminEmailTemplatesFixture WithBothTemplates() => new();

    public async ValueTask SetupEmptyAsync(EndToEndTestEnvironment environment)
    {
        await SeedTeamAndEventAsync(environment);
    }

    public async ValueTask<uint> SetupTeamTemplateAsync(EndToEndTestEnvironment environment)
    {
        var (team, _) = await SeedTeamAndEventAsync(environment);

        var template = new EmailTemplateBuilder()
            .ForTeam(team.Id)
            .WithType(TemplateType)
            .WithSubject("Team subject")
            .Build();

        await environment.EmailDatabase.SeedAsync(db => db.EmailTemplates.Add(template));
        return template.Version;
    }

    public async ValueTask<(uint TeamVersion, uint EventVersion)> SetupBothTemplatesAsync(EndToEndTestEnvironment environment)
    {
        var (team, eventId) = await SeedTeamAndEventAsync(environment);

        var teamTemplate = new EmailTemplateBuilder()
            .ForTeam(team.Id)
            .WithType(TemplateType)
            .WithSubject("Team subject")
            .Build();

        var eventTemplate = new EmailTemplateBuilder()
            .ForEvent(eventId)
            .WithType(TemplateType)
            .WithSubject("Event subject")
            .Build();

        await environment.EmailDatabase.SeedAsync(db =>
        {
            db.EmailTemplates.Add(teamTemplate);
            db.EmailTemplates.Add(eventTemplate);
        });

        return (teamTemplate.Version, eventTemplate.Version);
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
            Slug.From(EventSlug),
            DisplayName.From("Templates Conf"),
            AbsoluteUrl.From("https://example.com"),
            AbsoluteUrl.From("https://tickets.example.com"),
            DateTimeOffset.UtcNow.AddDays(60),
            DateTimeOffset.UtcNow.AddDays(61));

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
