using Amolenk.Admitto.Api.Tests.Infrastructure;
using Amolenk.Admitto.Api.Tests.Infrastructure.Hosting;
using Amolenk.Admitto.Module.Registrations.Domain.Entities;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;
using TeamBuilder = Amolenk.Admitto.Module.Organization.Tests.Application.Builders.TeamBuilder;

namespace Amolenk.Admitto.Api.Tests.Registrations.OtpRequest;

internal sealed class OtpRequestFixture
{
    public const string TeamSlug = "acme";
    public const string EventSlug = "devconf";
    public const string AttendeeEmail = "dave@example.com";

    public TeamId TeamId { get; private set; } = TeamId.New();
    public TicketedEventId EventId { get; private set; } = TicketedEventId.New();
    public string ApiKey => ApiKeyTestHelper.TestRawKey;

    public string RequestOtpRoute => $"/api/teams/{TeamSlug}/events/{EventSlug}/otp/request";

    private OtpRequestFixture() { }

    public static OtpRequestFixture ActiveEvent() => new();

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

        await environment.OrganizationDatabase.SeedAsync(db =>
        {
            db.Teams.Add(team);
            db.ApiKeys.Add(ApiKeyTestHelper.CreateApiKeyEntity(team.Id));
        });
        await environment.RegistrationsDatabase.SeedAsync(db => db.TicketedEvents.Add(ticketedEvent));
    }

    public async ValueTask SeedRateLimitedCodesAsync(
        EndToEndTestEnvironment environment,
        string email)
    {
        // Seed 3 OTP codes within the rate limit window to trigger the limit
        for (var i = 0; i < 3; i++)
        {
            var code = OtpCode.Create(TeamId, EventId, "DevConf", EmailAddress.From(email), "123456",
                DateTimeOffset.UtcNow.AddMinutes(10));
            await environment.RegistrationsDatabase.SeedAsync(db => db.OtpCodes.Add(code));
        }
    }
}
