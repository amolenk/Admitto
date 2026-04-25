using Amolenk.Admitto.Api.Tests.Infrastructure.Hosting;
using Amolenk.Admitto.Module.Email.Domain.Entities;
using Amolenk.Admitto.Module.Email.Domain.Tests.Builders;
using Amolenk.Admitto.Module.Email.Domain.ValueObjects;
using Amolenk.Admitto.Module.Registrations.Domain.Entities;
using Amolenk.Admitto.Module.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;
using TeamBuilder = Amolenk.Admitto.Module.Organization.Tests.Application.Builders.TeamBuilder;

namespace Amolenk.Admitto.Api.Tests.Email.BulkEmail;

/// <summary>
/// Shared fixture for the section-8 end-to-end bulk-email tests. Seeds a team,
/// a ticketed event, MailDev-backed email settings, and (optionally) a stored
/// template + a configurable set of registrations.
/// </summary>
internal sealed class BulkEmailFixture
{
    public const string TeamSlug = "acme-bulk";
    public const string EventSlug = "bulkconf";
    public const string TicketTypeSlug = "general-admission";
    public const string EmailType = EmailTemplateType.Ticket;
    public const string ReconfirmEmailType = EmailTemplateType.Reconfirm;

    public static string PreviewRoute =>
        $"/admin/teams/{TeamSlug}/events/{EventSlug}/bulk-emails/preview";

    public static string CreateRoute =>
        $"/admin/teams/{TeamSlug}/events/{EventSlug}/bulk-emails";

    public static string ListRoute => CreateRoute;

    public static string DetailRoute(Guid id) => $"{CreateRoute}/{id}";

    public static string CancelRoute(Guid id) => $"{CreateRoute}/{id}/cancel";

    public TeamId TeamId { get; private set; }
    public TicketedEventId EventId { get; private set; }
    public List<Registration> Registrations { get; } = [];

    private bool _seedTicketTemplate;
    private bool _seedReconfirmTemplate;
    private readonly List<RegistrationSeed> _registrationSeeds = [];

    private BulkEmailFixture() { }

    public static BulkEmailFixture Empty() => new();

    public BulkEmailFixture WithTicketTemplate()
    {
        _seedTicketTemplate = true;
        return this;
    }

    public BulkEmailFixture WithReconfirmTemplate()
    {
        _seedReconfirmTemplate = true;
        return this;
    }

    public BulkEmailFixture WithRegistration(
        string email,
        string firstName = "Test",
        string lastName = "User",
        bool reconfirmed = false,
        bool cancelled = false)
    {
        _registrationSeeds.Add(new RegistrationSeed(email, firstName, lastName, reconfirmed, cancelled));
        return this;
    }

    public async ValueTask SetupAsync(EndToEndTestEnvironment environment)
    {
        var team = new TeamBuilder().WithSlug(TeamSlug).Build();
        TeamId = team.Id;

        EventId = TicketedEventId.New();

        var ticketedEvent = TicketedEvent.Create(
            EventId,
            team.Id,
            Slug.From(EventSlug),
            DisplayName.From("Bulk Conf"),
            AbsoluteUrl.From("https://example.com"),
            AbsoluteUrl.From("https://tickets.example.com"),
            DateTimeOffset.UtcNow.AddDays(60),
            DateTimeOffset.UtcNow.AddDays(61),
            TimeZoneId.From("UTC"));

        ticketedEvent.ConfigureRegistrationPolicy(
            TicketedEventRegistrationPolicy.Create(
                DateTimeOffset.UtcNow.AddDays(-1),
                DateTimeOffset.UtcNow.AddDays(30)));

        var catalog = TicketCatalog.Create(EventId);
        catalog.AddTicketType(Slug.From(TicketTypeSlug), DisplayName.From("General Admission"), [], 100);

        // Seed registrations directly via the aggregate so we control state
        // (cancelled / reconfirmed) deterministically without going through the API.
        var ticketSnapshot = new TicketTypeSnapshot(TicketTypeSlug, []);
        foreach (var seed in _registrationSeeds)
        {
            var registration = Registration.Create(
                team.Id,
                EventId,
                EmailAddress.From(seed.Email),
                FirstName.From(seed.FirstName),
                LastName.From(seed.LastName),
                [ticketSnapshot]);

            if (seed.Reconfirmed)
                registration.Reconfirm(DateTimeOffset.UtcNow);

            if (seed.Cancelled)
                registration.Cancel(CancellationReason.AttendeeRequest);

            Registrations.Add(registration);
        }

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
            foreach (var registration in Registrations)
                db.Registrations.Add(registration);
        });

        await environment.EmailDatabase.SeedAsync(db =>
        {
            db.EmailSettings.Add(emailSettings);

            if (_seedTicketTemplate)
            {
                db.EmailTemplates.Add(new EmailTemplateBuilder()
                    .ForTeam(team.Id)
                    .WithType(EmailType)
                    .WithSubject("Hello {{ first_name }}")
                    .WithTextBody("Hi {{ first_name }} {{ last_name }}")
                    .WithHtmlBody("<p>Hi {{ first_name }} {{ last_name }}</p>")
                    .Build());
            }

            if (_seedReconfirmTemplate)
            {
                db.EmailTemplates.Add(new EmailTemplateBuilder()
                    .ForTeam(team.Id)
                    .WithType(ReconfirmEmailType)
                    .WithSubject("Please reconfirm")
                    .WithTextBody("Please reconfirm {{ first_name }}")
                    .WithHtmlBody("<p>Please reconfirm {{ first_name }}</p>")
                    .Build());
            }
        });
    }

    private sealed record RegistrationSeed(
        string Email,
        string FirstName,
        string LastName,
        bool Reconfirmed,
        bool Cancelled);
}
