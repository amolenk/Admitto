using Amolenk.Admitto.Api.Tests.Infrastructure;
using Amolenk.Admitto.Api.Tests.Infrastructure.Hosting;
using Amolenk.Admitto.Core.Registrations.Domain.Entities;
using Amolenk.Admitto.Core.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;
using TeamBuilder = Amolenk.Admitto.Testing.Builders.Organization.Application.TeamBuilder;

namespace Amolenk.Admitto.Api.Tests.Registrations.PartnerEventDetails;

internal sealed class PartnerEventDetailsFixture
{
    private readonly string? _allowedEmailDomain;
    private readonly IReadOnlyList<AdditionalDetailField> _additionalDetailFields;
    private readonly bool _registrationOpen;

    public TeamId TeamId { get; private set; } = TeamId.New();
    public TicketedEventId EventId { get; private set; } = TicketedEventId.New();
    public string EventSlug { get; private set; } = string.Empty;
    public string ApiKey => ApiKeyTestHelper.TestRawKey;

    public string EventDetailsRoute => $"/api/events/{EventSlug}";

    private PartnerEventDetailsFixture(
        bool registrationOpen,
        string? allowedEmailDomain,
        IReadOnlyList<AdditionalDetailField>? additionalDetailFields)
    {
        _registrationOpen = registrationOpen;
        _allowedEmailDomain = allowedEmailDomain;
        _additionalDetailFields = additionalDetailFields ?? [];
    }

    public static PartnerEventDetailsFixture Create(
        bool registrationOpen = true,
        string? allowedEmailDomain = null,
        IReadOnlyList<AdditionalDetailField>? additionalDetailFields = null) =>
        new(registrationOpen, allowedEmailDomain, additionalDetailFields);

    public async ValueTask SetupAsync(EndToEndTestEnvironment environment)
    {
        var team = new TeamBuilder()
            .Build();
        TeamId = team.Id;

        var eventId = TicketedEventId.New();
        EventId = eventId;

        var ticketedEvent = TicketedEvent.Create(
            CreationRequestId.From(Guid.NewGuid()),
            eventId,
            team.Id,
            EventName.From("DevConf"),
            AbsoluteUrl.From("https://example.com"),
            AbsoluteUrl.From("https://tickets.example.com"),
            DateTimeOffset.UtcNow.AddDays(60),
            DateTimeOffset.UtcNow.AddDays(61),
            TimeZoneId.From("Europe/Amsterdam"));
        EventSlug = ticketedEvent.PublicSlug.Value;

        // An open registration window (unless the scenario wants it closed) so IsRegistrationOpen is observable.
        var opensAt = _registrationOpen
            ? DateTimeOffset.UtcNow.AddDays(-1)
            : DateTimeOffset.UtcNow.AddDays(1);
        ticketedEvent.ConfigureRegistrationPolicy(TicketedEventRegistrationPolicy.Create(
            opensAt,
            DateTimeOffset.UtcNow.AddDays(30),
            _allowedEmailDomain));

        if (_additionalDetailFields.Count > 0)
        {
            ticketedEvent.UpdateAdditionalDetailSchema(_additionalDetailFields);
        }

        await environment.OrganizationDatabase.SeedAsync(db =>
        {
            db.Teams.Add(team);
            db.ApiKeys.Add(ApiKeyTestHelper.CreateApiKeyEntity(team.Id));
        });
        await environment.RegistrationsDatabase.SeedAsync(db => db.TicketedEvents.Add(ticketedEvent));
    }
}
