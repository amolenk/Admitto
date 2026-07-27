using System.Net.Http.Json;
using System.Text.Json;
using Amolenk.Admitto.Api.Tests.Infrastructure.Hosting;
using Amolenk.Admitto.Core.Email.Application.Projections.EventEmailContext;
using Amolenk.Admitto.Core.Email.Application.Projections.TeamEmailContext;
using Amolenk.Admitto.Core.Email.Application.Templating;
using Amolenk.Admitto.Core.Registrations.Contracts.IntegrationEvents;
using Amolenk.Admitto.Core.Registrations.Domain.Entities;
using Amolenk.Admitto.Core.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;
using TeamBuilder = Amolenk.Admitto.Testing.Builders.Organization.Application.TeamBuilder;

namespace Amolenk.Admitto.Api.Tests.Email.BulkEmail;

/// <summary>
/// Shared fixture for the section-8 end-to-end bulk-email tests. Seeds a team,
/// a ticketed event, MailDev-backed email settings, and (optionally) a stored
/// template + a configurable set of registrations.
/// </summary>
internal sealed class BulkEmailFixture
{
    public static readonly TicketTypeId TicketTypeId =
        TicketTypeId.From(new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"));

    public const string EmailType = BuiltInEmailTemplateNames.TicketConfirmation;
    public const string ReconfirmEmailType = BuiltInEmailTemplateNames.Reconfirmation;

    public string PreviewRoute =>
        $"/admin/teams/{TeamId.Value}/events/{EventId.Value}/bulk-emails/preview";

    public string CreateRoute =>
        $"/admin/teams/{TeamId.Value}/events/{EventId.Value}/bulk-emails";

    public string ListRoute => CreateRoute;

    public string DetailRoute(Guid id) => $"{CreateRoute}/{id}";

    public string CancelRoute(Guid id) => $"{CreateRoute}/{id}/cancel";

    public TeamId TeamId { get; private set; }
    public TicketedEventId EventId { get; private set; }
    public List<Registration> Registrations { get; } = [];

    private readonly List<RegistrationSeed> _registrationSeeds = [];

    private BulkEmailFixture()
    {
    }

    public static BulkEmailFixture Empty() => new();

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
        var team = new TeamBuilder().Build();
        TeamId = team.Id;

        EventId = TicketedEventId.New();

        var ticketedEvent = TicketedEvent.Create(
            CreationRequestId.From(Guid.NewGuid()),
            EventId,
            team.Id,
            EventName.From("Bulk Conf"),
            AbsoluteUrl.From("https://example.com"),
            AbsoluteUrl.From("https://tickets.example.com"),
            DateTimeOffset.UtcNow.AddDays(60),
            DateTimeOffset.UtcNow.AddDays(61),
            TimeZoneId.From("UTC"));

        ticketedEvent.ConfigureRegistrationPolicy(
            TicketedEventRegistrationPolicy.Create(
                DateTimeOffset.UtcNow.AddDays(-1),
                DateTimeOffset.UtcNow.AddDays(30)));

        var catalog = TicketCatalog.Create(EventId, team.Id);
        catalog.AddTicketType(TicketTypeId, TicketTypeName.From("General Admission"), [], 100);

        // Seed registrations directly via the aggregate so we control state
        // (cancelled / reconfirmed) deterministically without going through the API.
        var ticketSnapshot = new TicketTypeSnapshot(TicketTypeId, TicketTypeName.From("General Admission"), []);
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

        await environment.OrganizationDatabase.SeedAsync(db => db.Teams.Add(team));
        await environment.RegistrationsDatabase.SeedAsync(db =>
        {
            db.TicketedEvents.Add(ticketedEvent);
            db.TicketCatalogs.Add(catalog);
            foreach (var registration in Registrations)
                db.Registrations.Add(registration);
        });

        // Seed the Email-owned rendering projection directly. In production this
        // row is maintained by EventEmailContextProjector from Organization /
        // Registrations integration events; the fixture seeds the events
        // synchronously, so we materialise the equivalent projection state here.
        await environment.EmailDatabase.SeedAsync(db =>
        {
            var context = EventEmailContextView.CreatePartial(
                TeamId, EventId, DateTimeOffset.UtcNow);
            context.UpdateEventContext(
                ticketedEventVersion: 0,
                "Bulk Conf",
                "https://example.com",
                ticketedEvent.PublicSlug.Value,
                "UTC",
                selfServiceTicketTypeCount: 1,
                reconfirmPolicy: new TicketedEventReconfirmPolicySnapshot(
                    DateTimeOffset.UtcNow.AddDays(-1),
                    DateTimeOffset.UtcNow.AddDays(30),
                    CadenceHours: 24,
                    MinEmailIntervalHours: 24),
                isArchived: false,
                DateTimeOffset.UtcNow);
            var teamContext = TeamEmailContextView.Create(
                TeamId,
                team.Name.Value,
                team.AccentColor.Value,
                team.Version,
                DateTimeOffset.UtcNow);
            db.EventEmailContexts.Add(context);
            db.TeamEmailContexts.Add(teamContext);
        });
    }

    public async Task<JsonElement> PollUntilTerminalAsync(
        Guid bulkJobId,
        EndToEndTestEnvironment environment,
        CancellationToken cancellationToken = default)
    {
        var deadline = DateTimeOffset.UtcNow.AddSeconds(60);
        JsonElement last = default;
        while (DateTimeOffset.UtcNow < deadline)
        {
            var response = await environment.ApiClient.GetAsync(
                DetailRoute(bulkJobId),
                cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                last = await response.Content.ReadFromJsonAsync<JsonElement>(
                    cancellationToken: cancellationToken);
                var status = last.GetProperty("status").GetString();
                if (status is "completed" or "partiallyFailed" or "failed" or "cancelled")
                    return last;
            }

            await Task.Delay(TimeSpan.FromMilliseconds(500), cancellationToken);
        }

        throw new TimeoutException(
            $"Bulk-email job {bulkJobId} never reached a terminal state. Last observed: {last}");
    }

    private sealed record RegistrationSeed(
        string Email,
        string FirstName,
        string LastName,
        bool Reconfirmed,
        bool Cancelled);
}
