using Amolenk.Admitto.Api.Tests.Infrastructure;
using Amolenk.Admitto.Api.Tests.Infrastructure.Hosting;
using Amolenk.Admitto.Module.Registrations.Domain.Entities;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;
using TeamBuilder = Amolenk.Admitto.Module.Organization.Tests.Application.Builders.TeamBuilder;

namespace Amolenk.Admitto.Api.Tests.Registrations.OtpVerify;

internal sealed class OtpVerifyFixture
{
    public const string TeamSlug = "acme";
    public const string EventSlug = "devconf";
    public const string AttendeeEmail = "dave@example.com";
    public const string KnownPlainCode = "123456";

    public TeamId TeamId { get; private set; } = TeamId.New();
    public TicketedEventId EventId { get; private set; } = TicketedEventId.New();
    public string ApiKey => ApiKeyTestHelper.TestRawKey;

    public string VerifyOtpRoute => $"/api/teams/{TeamSlug}/events/{EventSlug}/otp/verify";

    private OtpVerifyFixture() { }

    public static OtpVerifyFixture WithActiveCode() => new();

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

    public async ValueTask SeedValidCodeAsync(EndToEndTestEnvironment environment, string? email = null)
    {
        email ??= AttendeeEmail;
        var otpCode = OtpCode.Create(TeamId, EventId, "DevConf",
            EmailAddress.From(email), KnownPlainCode,
            DateTimeOffset.UtcNow.AddMinutes(10));

        await environment.RegistrationsDatabase.SeedAsync(db => db.OtpCodes.Add(otpCode));
    }

    public async ValueTask SeedExpiredCodeAsync(EndToEndTestEnvironment environment)
    {
        var otpCode = OtpCode.Create(TeamId, EventId, "DevConf",
            EmailAddress.From(AttendeeEmail), KnownPlainCode,
            DateTimeOffset.UtcNow.AddMinutes(-1));

        await environment.RegistrationsDatabase.SeedAsync(db => db.OtpCodes.Add(otpCode));
    }

    public async ValueTask SeedUsedCodeAsync(EndToEndTestEnvironment environment)
    {
        var otpCode = OtpCode.Create(TeamId, EventId, "DevConf",
            EmailAddress.From(AttendeeEmail), KnownPlainCode,
            DateTimeOffset.UtcNow.AddMinutes(10));
        otpCode.MarkUsed(DateTimeOffset.UtcNow);

        await environment.RegistrationsDatabase.SeedAsync(db => db.OtpCodes.Add(otpCode));
    }

    public async ValueTask SeedLockedCodeAsync(EndToEndTestEnvironment environment)
    {
        var otpCode = OtpCode.Create(TeamId, EventId, "DevConf",
            EmailAddress.From(AttendeeEmail), KnownPlainCode,
            DateTimeOffset.UtcNow.AddMinutes(10));

        // Simulate 4 prior failures (so 5th attempt triggers lock)
        for (var i = 0; i < 4; i++)
            otpCode.IncrementFailedAttempts();

        await environment.RegistrationsDatabase.SeedAsync(db => db.OtpCodes.Add(otpCode));
    }
}
