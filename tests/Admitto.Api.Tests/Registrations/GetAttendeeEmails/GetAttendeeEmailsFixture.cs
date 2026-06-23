using Amolenk.Admitto.Api.Tests.Infrastructure.Hosting;
using Amolenk.Admitto.Core.Email.Domain.Entities;
using Amolenk.Admitto.Core.Email.Domain.ValueObjects;
using Amolenk.Admitto.Core.Registrations.Domain.Entities;
using Amolenk.Admitto.Core.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;
using Amolenk.Admitto.Core.Registrations.Contracts.ValueObjects;
using TeamBuilder = Amolenk.Admitto.Testing.Builders.Organization.Application.TeamBuilder;

namespace Amolenk.Admitto.Api.Tests.Registrations.GetAttendeeEmails;

internal sealed class GetAttendeeEmailsFixture
{
    public Guid TeamId { get; private set; }
    public Guid EventId { get; private set; }

    public RegistrationId RegistrationId { get; private set; } = RegistrationId.New();

    private bool _withEmails;

    private GetAttendeeEmailsFixture() { }

    public static GetAttendeeEmailsFixture Empty() => new();

    public static GetAttendeeEmailsFixture WithEmails() => new() { _withEmails = true };

    public string Route =>
        $"/admin/teams/{TeamId}/events/{EventId}/registrations/{RegistrationId.Value}/emails";

    public async ValueTask SetupAsync(EndToEndTestEnvironment environment)
    {
        var team = new TeamBuilder()
            .Build();
        TeamId = team.Id.Value;

        var eventId = TicketedEventId.New();
        EventId = eventId.Value;

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

        var registration = Registration.Create(
            team.Id,
            eventId,
            EmailAddress.From("alice@example.com"),
            FirstName.From("Alice"),
            LastName.From("Doe"),
            [new TicketTypeSnapshot(TicketTypeId.From(new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa")), TicketTypeName.From("General Admission"), [])]);
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
                teamId: team.Id,
                ticketedEventId: eventId,
                idempotencyKey: "confirmation-key",
                recipient: EmailAddress.From("alice@example.com"),
                emailType: "Confirmation",
                subject: "Your DevConf registration",
                status: EmailLogStatus.Sent,
                sentAt: sentAt,
                statusUpdatedAt: sentAt,
                registrationId: registration.Id);

            await environment.EmailDatabase.SeedAsync(db => db.EmailLog.Add(emailLog));
        }
    }
}
