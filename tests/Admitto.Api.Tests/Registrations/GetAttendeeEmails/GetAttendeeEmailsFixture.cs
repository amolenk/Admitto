using Amolenk.Admitto.Api.Tests.Infrastructure.Hosting;
using Amolenk.Admitto.Module.Email.Domain.Entities;
using Amolenk.Admitto.Module.Email.Domain.ValueObjects;
using Amolenk.Admitto.Module.Registrations.Domain.Entities;
using Amolenk.Admitto.Module.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;
using TeamBuilder = Amolenk.Admitto.Module.Organization.Tests.Application.Builders.TeamBuilder;

namespace Amolenk.Admitto.Api.Tests.Registrations.GetAttendeeEmails;

internal sealed class GetAttendeeEmailsFixture
{
    public const string TeamSlug = "acme";
    public const string EventSlug = "devconf";

    public RegistrationId RegistrationId { get; private set; } = RegistrationId.New();

    private bool _withEmails;

    private GetAttendeeEmailsFixture() { }

    public static GetAttendeeEmailsFixture Empty() => new();

    public static GetAttendeeEmailsFixture WithEmails() => new() { _withEmails = true };

    public string Route =>
        $"/admin/teams/{TeamSlug}/events/{EventSlug}/registrations/{RegistrationId.Value}/emails";

    public async ValueTask SetupAsync(EndToEndTestEnvironment environment)
    {
        var team = new TeamBuilder()
            .WithSlug(TeamSlug)
            .Build();

        var eventId = TicketedEventId.New();

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
            EmailAddress.From("alice@example.com"),
            FirstName.From("Alice"),
            LastName.From("Doe"),
            [new TicketTypeSnapshot("general-admission", "General Admission", [])]);
        RegistrationId = registration.Id;

        await environment.OrganizationDatabase.SeedAsync(db => db.Teams.Add(team));
        await environment.RegistrationsDatabase.SeedAsync(db =>
        {
            db.TicketedEvents.Add(ticketedEvent);
            db.Registrations.Add(registration);
        });

        if (_withEmails)
        {
            var sentAt = DateTimeOffset.UtcNow.AddHours(-1);
            var emailLog = EmailLog.Create(
                teamId: team.Id.Value,
                ticketedEventId: eventId.Value,
                idempotencyKey: "confirmation-key",
                recipient: "alice@example.com",
                emailType: "Confirmation",
                subject: "Your DevConf registration",
                provider: "test",
                providerMessageId: null,
                status: EmailLogStatus.Sent,
                sentAt: sentAt,
                statusUpdatedAt: sentAt,
                registrationId: registration.Id.Value);

            await environment.EmailDatabase.SeedAsync(db => db.EmailLog.Add(emailLog));
        }
    }
}
