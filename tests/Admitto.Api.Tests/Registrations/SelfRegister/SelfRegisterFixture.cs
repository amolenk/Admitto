using System.Net.Http.Json;
using System.Text.Json;
using Amolenk.Admitto.Api.Tests.Infrastructure;
using Amolenk.Admitto.Api.Tests.Infrastructure.Hosting;
using Amolenk.Admitto.Module.Registrations.Domain.Entities;
using Amolenk.Admitto.Module.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;
using TeamBuilder = Amolenk.Admitto.Module.Organization.Tests.Application.Builders.TeamBuilder;

namespace Amolenk.Admitto.Api.Tests.Registrations.SelfRegister;

internal sealed class SelfRegisterFixture
{
    public const string TeamSlug = "acme";
    public const string EventSlug = "devconf";
    public const string AttendeeEmail = "dave@example.com";
    private const string KnownOtpCode = "654321";

    public TeamId TeamId { get; private set; } = TeamId.New();
    public TicketedEventId EventId { get; private set; } = TicketedEventId.New();
    public string ApiKey => ApiKeyTestHelper.TestRawKey;

    public string RegisterRoute => $"/api/teams/{TeamSlug}/events/{EventSlug}/registrations";
    public string OtpVerifyRoute => $"/api/teams/{TeamSlug}/events/{EventSlug}/otp/verify";

    private SelfRegisterFixture() { }

    public static SelfRegisterFixture WithOpenRegistration() => new();

    public async ValueTask SetupAsync(EndToEndTestEnvironment environment)
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

        ticketedEvent.ConfigureRegistrationPolicy(TicketedEventRegistrationPolicy.Create(
            DateTimeOffset.UtcNow.AddDays(-1),
            DateTimeOffset.UtcNow.AddDays(30)));

        var catalog = TicketCatalog.Create(eventId);
        catalog.AddTicketType(Slug.From("general-admission"), DisplayName.From("General Admission"), [], 100);

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

    /// <summary>Seeds a valid OTP code and retrieves a verification token via the OTP verify endpoint.</summary>
    public async Task<string> GetVerificationTokenAsync(
        EndToEndTestEnvironment environment,
        CancellationToken cancellationToken = default,
        string? email = null)
    {
        email ??= AttendeeEmail;

        var otpCode = OtpCode.Create(TeamId, EventId, "DevConf",
            EmailAddress.From(email), KnownOtpCode,
            DateTimeOffset.UtcNow.AddMinutes(10));
        await environment.RegistrationsDatabase.SeedAsync(db => db.OtpCodes.Add(otpCode));

        using var client = environment.CreatePublicApiClient(ApiKeyTestHelper.TestRawKey);
        var response = await client.PostAsJsonAsync(
            OtpVerifyRoute,
            new { Email = email, Code = KnownOtpCode },
            cancellationToken: cancellationToken);

        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: cancellationToken);
        return body.GetProperty("token").GetString()!;
    }
}
