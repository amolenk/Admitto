using Amolenk.Admitto.Api.Tests.Infrastructure;
using Amolenk.Admitto.Api.Tests.Infrastructure.Hosting;
using Amolenk.Admitto.Core.Organization.Domain.Entities;
using Amolenk.Admitto.Core.Registrations.Domain.Entities;
using Amolenk.Admitto.Core.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;
using Amolenk.Admitto.Core.Registrations.Contracts.ValueObjects;
using TeamBuilder = Amolenk.Admitto.Testing.Builders.Organization.Application.TeamBuilder;

namespace Amolenk.Admitto.Api.Tests.Registrations.GetRegistrationDetail;

internal sealed class GetRegistrationDetailFixture
{
    public static readonly TicketTypeId TicketTypeId = TicketTypeId.From(new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"));
    public const string KnownPlainCode = "123456";

    public Guid TeamId { get; private set; }
    public Guid EventId { get; private set; }
    public string EventSlug { get; private set; } = string.Empty;

    public RegistrationId RegistrationId { get; private set; } = RegistrationId.New();
    public RegistrationId OtherEventRegistrationId { get; private set; } = RegistrationId.New();
    public string ApiKey => ApiKeyTestHelper.TestRawKey;
    public string OtherTeamApiKey => ApiKeyTestHelper.TestRawKey2;

    private readonly bool _withAdditionalDetails;
    private readonly bool _seedOtherTeamApiKey;

    private GetRegistrationDetailFixture(
        bool withAdditionalDetails = false,
        bool seedOtherTeamApiKey = false)
    {
        _withAdditionalDetails = withAdditionalDetails;
        _seedOtherTeamApiKey = seedOtherTeamApiKey;
    }

    public static GetRegistrationDetailFixture WithActiveRegistration() => new();

    public static GetRegistrationDetailFixture WithPartnerRegistration() =>
        new(withAdditionalDetails: true);

    public static GetRegistrationDetailFixture WithPartnerRegistrationAndOtherTeamApiKey() =>
        new(withAdditionalDetails: true, seedOtherTeamApiKey: true);

    public string RegistrationRoute =>
        $"/admin/teams/{TeamId}/events/{EventId}/registrations/{RegistrationId.Value}";

    public string PartnerRegistrationRoute =>
        $"/api/events/{EventSlug}/registrations/{RegistrationId.Value}";

    public string ResolvePartnerRegistrationRoute(string email) =>
        $"/api/events/{EventSlug}/registrations/resolve?email={Uri.EscapeDataString(email)}";

    public string VerifyOtpRoute => $"/api/events/{EventSlug}/otp/verify";

    public string PartnerRegistrationRouteFor(RegistrationId registrationId) =>
        $"/api/events/{EventSlug}/registrations/{registrationId.Value}";

    public async ValueTask SetupAsync(EndToEndTestEnvironment environment)
    {
        var team = new TeamBuilder()
            .Build();
        TeamId = team.Id.Value;

        var eventId = TicketedEventId.New();
        EventId = eventId.Value;
        var otherEventId = TicketedEventId.New();

        var ticketedEvent = TicketedEvent.Create(
            CreationRequestId.From(Guid.NewGuid()),
            eventId,
            team.Id,
            EventName.From("DevConf"),
            AbsoluteUrl.From("https://example.com"),
            AbsoluteUrl.From("https://tickets.example.com"),
            DateTimeOffset.UtcNow.AddDays(60),
            DateTimeOffset.UtcNow.AddDays(61),
            TimeZoneId.From("UTC"));
        EventSlug = ticketedEvent.PublicSlug.Value;

        var otherTicketedEvent = TicketedEvent.Create(
            CreationRequestId.From(Guid.NewGuid()),
            otherEventId,
            team.Id,
            EventName.From("OtherConf"),
            AbsoluteUrl.From("https://other.example.com"),
            AbsoluteUrl.From("https://other-tickets.example.com"),
            DateTimeOffset.UtcNow.AddDays(90),
            DateTimeOffset.UtcNow.AddDays(91),
            TimeZoneId.From("UTC"));

        AdditionalDetails? additionalDetails = null;
        if (_withAdditionalDetails)
            additionalDetails = AdditionalDetails.From(
                new Dictionary<string, string> { { "dietary", "vegan" } });

        var registration = Registration.Create(
            team.Id,
            eventId,
            EmailAddress.From("alice@example.com"),
            FirstName.From("Alice"),
            LastName.From("Doe"),
            [new TicketTypeSnapshot(TicketTypeId, TicketTypeName.From("General Admission"), [])],
            additionalDetails);
        RegistrationId = registration.Id;

        var otherEventRegistration = Registration.Create(
            team.Id,
            otherEventId,
            EmailAddress.From("bob@example.com"),
            FirstName.From("Bob"),
            LastName.From("Smith"),
            [new TicketTypeSnapshot(TicketTypeId, TicketTypeName.From("General Admission"), [])]);
        OtherEventRegistrationId = otherEventRegistration.Id;

        Team? otherTeam = null;
        ApiKey? otherApiKey = null;
        if (_seedOtherTeamApiKey)
        {
            otherTeam = new TeamBuilder().Build();
            otherApiKey = ApiKeyTestHelper.CreateApiKeyEntity2(otherTeam.Id);
        }

        await environment.OrganizationDatabase.SeedAsync(db =>
        {
            db.Teams.Add(team);
            db.ApiKeys.Add(ApiKeyTestHelper.CreateApiKeyEntity(team.Id));
            if (otherTeam is not null)
                db.Teams.Add(otherTeam);
            if (otherApiKey is not null)
                db.ApiKeys.Add(otherApiKey);
        });
        await environment.RegistrationsDatabase.SeedAsync(db =>
        {
            db.TicketedEvents.Add(ticketedEvent);
            db.TicketedEvents.Add(otherTicketedEvent);
            db.Registrations.Add(registration);
            db.Registrations.Add(otherEventRegistration);
        });
    }

    public async ValueTask SeedValidCodeAsync(EndToEndTestEnvironment environment, string email = "alice@example.com")
    {
        var otpCode = OtpCode.Create(
            Amolenk.Admitto.Core.Shared.Kernel.ValueObjects.TeamId.From(TeamId),
            TicketedEventId.From(EventId),
            EventName.From("DevConf"),
            EmailAddress.From(email),
            KnownPlainCode,
            DateTimeOffset.UtcNow.AddMinutes(10));

        await environment.RegistrationsDatabase.SeedAsync(db => db.OtpCodes.Add(otpCode));
    }
}
