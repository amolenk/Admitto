using Amolenk.Admitto.Api.Tests.Infrastructure;
using Amolenk.Admitto.Api.Tests.Infrastructure.Hosting;
using Amolenk.Admitto.Core.Email.Application.Projections.EventEmailContext;
using Amolenk.Admitto.Core.Email.Application.Projections.TeamEmailContext;
using Amolenk.Admitto.Core.Organization.Domain.Entities;
using Amolenk.Admitto.Core.Registrations.Contracts.ValueObjects;
using Amolenk.Admitto.Core.Registrations.Domain.Entities;
using Amolenk.Admitto.Core.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;
using TeamBuilder = Amolenk.Admitto.Testing.Builders.Organization.Application.TeamBuilder;

namespace Amolenk.Admitto.Api.Tests.Email.RequestTicketConfirmationResend;

internal sealed class RequestTicketConfirmationResendFixture
{
    public static readonly TicketTypeId TicketTypeId = TicketTypeId.From(new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"));

    public Guid TeamId { get; private set; }
    public Guid EventId { get; private set; }
    public string EventSlug { get; private set; } = string.Empty;
    public RegistrationId RegistrationId { get; private set; } = RegistrationId.New();
    public string ApiKey => ApiKeyTestHelper.TestRawKey;
    public string OtherTeamApiKey => ApiKeyTestHelper.TestRawKey2;

    private readonly bool _cancelled;
    private readonly bool _revokeApiKey;
    private readonly bool _seedOtherTeamApiKey;

    private RequestTicketConfirmationResendFixture(
        bool cancelled,
        bool revokeApiKey = false,
        bool seedOtherTeamApiKey = false)
    {
        _cancelled = cancelled;
        _revokeApiKey = revokeApiKey;
        _seedOtherTeamApiKey = seedOtherTeamApiKey;
    }

    public static RequestTicketConfirmationResendFixture RegisteredAttendee() => new(cancelled: false);

    public static RequestTicketConfirmationResendFixture CancelledAttendee() => new(cancelled: true);

    public static RequestTicketConfirmationResendFixture RegisteredAttendeeWithRevokedApiKey() =>
        new(cancelled: false, revokeApiKey: true);

    public static RequestTicketConfirmationResendFixture RegisteredAttendeeWithOtherTeamApiKey() =>
        new(cancelled: false, seedOtherTeamApiKey: true);

    public string ResendRoute =>
        $"/admin/teams/{TeamId}/events/{EventId}/registrations/{RegistrationId.Value}/ticket-email/resend";

    public string PartnerResendRoute =>
        $"/api/events/{EventSlug}/registrations/{RegistrationId.Value}/ticket-email/resend";

    public string EmailsRoute =>
        $"/admin/teams/{TeamId}/events/{EventId}/registrations/{RegistrationId.Value}/emails";

    public async ValueTask SetupAsync(EndToEndTestEnvironment environment)
    {
        var team = new TeamBuilder().Build();
        var eventId = TicketedEventId.New();

        TeamId = team.Id.Value;
        EventId = eventId.Value;

        var ticketedEvent = TicketedEvent.Create(
            CreationRequestId.From(Guid.NewGuid()),
            eventId,
            team.Id,
            EventName.From("MailConf"),
            AbsoluteUrl.From("https://example.com"),
            AbsoluteUrl.From("https://tickets.example.com"),
            DateTimeOffset.UtcNow.AddDays(60),
            DateTimeOffset.UtcNow.AddDays(61),
            TimeZoneId.From("UTC"));
        EventSlug = ticketedEvent.PublicSlug.Value;

        var registration = Registration.Create(
            team.Id,
            eventId,
            EmailAddress.From("alice@example.com"),
            FirstName.From("Alice"),
            LastName.From("Doe"),
            [new TicketTypeSnapshot(TicketTypeId, TicketTypeName.From("General Admission"), [])]);

        if (_cancelled)
            registration.Cancel(CancellationReason.AttendeeRequest);

        RegistrationId = registration.Id;

        var apiKey = ApiKeyTestHelper.CreateApiKeyEntity(team.Id);
        if (_revokeApiKey)
            apiKey.Revoke(DateTimeOffset.UtcNow.AddMinutes(-1));

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
            db.ApiKeys.Add(apiKey);
            if (otherTeam is not null)
                db.Teams.Add(otherTeam);
            if (otherApiKey is not null)
                db.ApiKeys.Add(otherApiKey);
        });
        await environment.RegistrationsDatabase.SeedAsync(db =>
        {
            db.TicketedEvents.Add(ticketedEvent);
            db.Registrations.Add(registration);
        });

        await environment.EmailDatabase.SeedAsync(db =>
        {
            var context = EventEmailContextView.CreatePartial(team.Id, eventId, DateTimeOffset.UtcNow);
            context.UpdateEventContext(
                ticketedEventVersion: 0,
                "MailConf",
                "https://example.com",
                ticketedEvent.PublicSlug.Value,
                "UTC",
                selfServiceTicketTypeCount: 1,
                reconfirmPolicy: null,
                isArchived: false,
                DateTimeOffset.UtcNow);

            var teamContext = TeamEmailContextView.CreatePartial(team.Id, DateTimeOffset.UtcNow);
            teamContext.UpdateTeamContext(
                team.Name.Value,
                team.AccentColor.Value,
                team.ReplyToEmailAddress?.Value,
                team.Version,
                DateTimeOffset.UtcNow);

            db.EventEmailContexts.Add(context);
            db.TeamEmailContexts.Add(teamContext);
        });
    }
}
