using System.Net.Http.Json;
using System.Text.Json;
using Amolenk.Admitto.Api.Tests.Infrastructure;
using Amolenk.Admitto.Api.Tests.Infrastructure.Hosting;
using Amolenk.Admitto.Module.Registrations.Domain.Entities;
using Amolenk.Admitto.Module.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;
using TeamBuilder = Amolenk.Admitto.Module.Organization.Tests.Application.Builders.TeamBuilder;

namespace Amolenk.Admitto.Api.Tests.Registrations.SelfCancelRegistration;

internal sealed class SelfCancelRegistrationFixture
{
    public const string TeamSlug = "acme";
    public const string EventSlug = "devconf";
    public const string AttendeeEmail = "alice@example.com";
    private const string KnownOtpCode = "789012";

    public TeamId TeamId { get; private set; } = TeamId.New();
    public TicketedEventId EventId { get; private set; } = TicketedEventId.New();
    public RegistrationId RegistrationId { get; private set; } = RegistrationId.New();
    public string ApiKey => ApiKeyTestHelper.TestRawKey;

    public string CancelRoute => $"/api/teams/{TeamSlug}/events/{EventSlug}/registrations/{RegistrationId.Value}/cancel";
    private string OtpVerifyRoute => $"/api/teams/{TeamSlug}/events/{EventSlug}/otp/verify";

    private SelfCancelRegistrationFixture() { }

    public static SelfCancelRegistrationFixture WithActiveRegistration() => new();
    public static SelfCancelRegistrationFixture WithCancelledRegistration() => new();

    public async ValueTask SetupAsync(EndToEndTestEnvironment environment, bool alreadyCancelled = false)
    {
        var team = new TeamBuilder()
            .WithSlug(TeamSlug)
            .Build();
        TeamId = team.Id;

        var eventId = TicketedEventId.New();
        EventId = eventId;

        var ticketedEvent = TicketedEvent.Create(
            eventId,
            team.Id,
            Slug.From(TeamSlug),
            Slug.From(EventSlug),
            DisplayName.From("DevConf"),
            AbsoluteUrl.From("https://example.com"),
            AbsoluteUrl.From("https://tickets.example.com"),
            DateTimeOffset.UtcNow.AddDays(60),
            DateTimeOffset.UtcNow.AddDays(61),
            TimeZoneId.From("UTC"));

        var registration = Registration.Create(
            team.Id,
            eventId,
            EmailAddress.From(AttendeeEmail),
            FirstName.From("Alice"),
            LastName.From("Test"),
            [new TicketTypeSnapshot("general-admission", "General Admission", [])]);
        RegistrationId = registration.Id;

        if (alreadyCancelled)
            registration.Cancel(CancellationReason.AttendeeRequest);

        await environment.OrganizationDatabase.SeedAsync(db =>
        {
            db.Teams.Add(team);
            db.ApiKeys.Add(ApiKeyTestHelper.CreateApiKeyEntity(team.Id));
        });
        await environment.RegistrationsDatabase.SeedAsync(db =>
        {
            db.TicketedEvents.Add(ticketedEvent);
            db.Registrations.Add(registration);
        });
    }

    public async Task<string> GetVerificationTokenAsync(
        EndToEndTestEnvironment environment,
        CancellationToken cancellationToken = default)
    {
        var otpCode = OtpCode.Create(TeamId, EventId, "DevConf",
            EmailAddress.From(AttendeeEmail), KnownOtpCode,
            DateTimeOffset.UtcNow.AddMinutes(10));
        await environment.RegistrationsDatabase.SeedAsync(db => db.OtpCodes.Add(otpCode));

        using var client = environment.CreatePublicApiClient(ApiKeyTestHelper.TestRawKey);
        var response = await client.PostAsJsonAsync(
            OtpVerifyRoute,
            new { Email = AttendeeEmail, Code = KnownOtpCode },
            cancellationToken: cancellationToken);

        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: cancellationToken);
        return body.GetProperty("token").GetString()!;
    }
}
