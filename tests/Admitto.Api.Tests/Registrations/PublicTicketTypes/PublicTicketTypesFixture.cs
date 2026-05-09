using Amolenk.Admitto.Api.Tests.Infrastructure;
using Amolenk.Admitto.Api.Tests.Infrastructure.Hosting;
using Amolenk.Admitto.Core.Module.Registrations.Domain.Entities;
using Amolenk.Admitto.Core.Module.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;
using TeamBuilder = Amolenk.Admitto.Core.Module.Organization.Tests.Application.Builders.TeamBuilder;

namespace Amolenk.Admitto.Api.Tests.Registrations.PublicTicketTypes;

internal sealed class PublicTicketTypesFixture
{
    public TeamId TeamId { get; private set; } = TeamId.New();
    public TicketedEventId EventId { get; private set; } = TicketedEventId.New();
    public string ApiKey => ApiKeyTestHelper.TestRawKey;

    public string TicketTypesRoute => $"/api/teams/{TeamId.Value}/events/{EventId.Value}/ticket-types";

    private PublicTicketTypesFixture() { }

    public static PublicTicketTypesFixture Create() => new();

    public async ValueTask SetupAsync(
        EndToEndTestEnvironment environment,
        Action<TicketCatalog>? configureCatalog = null)
    {
        var team = new TeamBuilder()
            .Build();
        TeamId = team.Id;

        var eventId = TicketedEventId.New();
        EventId = eventId;

        var ticketedEvent = TicketedEvent.Create(
            eventId,
            team.Id,
            DisplayName.From("DevConf"),
            AbsoluteUrl.From("https://example.com"),
            AbsoluteUrl.From("https://tickets.example.com"),
            DateTimeOffset.UtcNow.AddDays(60),
            DateTimeOffset.UtcNow.AddDays(61),
            TimeZoneId.From("UTC"));

        ticketedEvent.ConfigureRegistrationPolicy(TicketedEventRegistrationPolicy.Create(
            DateTimeOffset.UtcNow.AddDays(-1),
            DateTimeOffset.UtcNow.AddDays(30)));

        var catalog = TicketCatalog.Create(eventId);
        configureCatalog?.Invoke(catalog);

        await environment.OrganizationDatabase.SeedAsync(db =>
        {
            db.Teams.Add(team);
            db.ApiKeys.Add(ApiKeyTestHelper.CreateApiKeyEntity(team.Id));
        });
        await environment.RegistrationsDatabase.SeedAsync(db =>
        {
            db.TicketedEvents.Add(ticketedEvent);
            db.TicketCatalogs.Add(catalog);
        });
    }
}
