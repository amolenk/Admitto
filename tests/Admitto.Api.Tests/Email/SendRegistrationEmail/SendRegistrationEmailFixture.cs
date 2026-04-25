using Amolenk.Admitto.Api.Tests.Infrastructure.Hosting;
using Amolenk.Admitto.Module.Email.Domain.Entities;
using Amolenk.Admitto.Module.Email.Domain.ValueObjects;
using Amolenk.Admitto.Module.Registrations.Domain.Entities;
using Amolenk.Admitto.Module.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;
using TeamBuilder = Amolenk.Admitto.Module.Organization.Tests.Application.Builders.TeamBuilder;

namespace Amolenk.Admitto.Api.Tests.Email.SendRegistrationEmail;

internal sealed class SendRegistrationEmailFixture
{
    public const string TeamSlug = "acme-email";
    public const string EventSlug = "mailconf";
    public const string TicketTypeSlug = "general-admission";
    public const string RecipientEmail = "attendee@example.com";

    public static string RegisterRoute =>
        $"/admin/teams/{TeamSlug}/events/{EventSlug}/registrations";

    private SendRegistrationEmailFixture() { }

    public static SendRegistrationEmailFixture HappyFlow() => new();

    public async ValueTask SetupAsync(EndToEndTestEnvironment environment)
    {
        var team = new TeamBuilder()
            .WithSlug(TeamSlug)
            .Build();

        var eventId = TicketedEventId.New();

        var ticketedEvent = TicketedEvent.Create(
            eventId,
            team.Id,
            Slug.From(EventSlug),
            DisplayName.From("MailConf"),
            AbsoluteUrl.From("https://example.com"),
            AbsoluteUrl.From("https://tickets.example.com"),
            DateTimeOffset.UtcNow.AddDays(60),
            DateTimeOffset.UtcNow.AddDays(61),
                TimeZoneId.From("UTC"));
        ticketedEvent.ConfigureRegistrationPolicy(
            TicketedEventRegistrationPolicy.Create(
                DateTimeOffset.UtcNow.AddDays(-1),
                DateTimeOffset.UtcNow.AddDays(30)));

        var catalog = TicketCatalog.Create(eventId);
        catalog.AddTicketType(Slug.From(TicketTypeSlug), DisplayName.From("General Admission"), [], 100);

        // Seed team-scoped email settings pointing at MailDev SMTP.
        // Use the dynamic endpoint from the test environment to avoid port conflicts.
        var smtpHost = environment.MailDevSmtpEndpoint.Host;
        var smtpPort = environment.MailDevSmtpEndpoint.Port;

        var emailSettings = EmailSettings.Create(
            scope: EmailSettingsScope.Team,
            scopeId: team.Id.Value,
            smtpHost: Hostname.From(smtpHost),
            smtpPort: Port.From(smtpPort),
            fromAddress: EmailAddress.From("noreply@admitto.io"),
            authMode: EmailAuthMode.None,
            username: null,
            protectedPassword: null);

        await environment.OrganizationDatabase.SeedAsync(db => db.Teams.Add(team));
        await environment.RegistrationsDatabase.SeedAsync(db =>
        {
            db.TicketedEvents.Add(ticketedEvent);
            db.TicketCatalogs.Add(catalog);
        });
        await environment.EmailDatabase.SeedAsync(db => db.EmailSettings.Add(emailSettings));
    }
}
