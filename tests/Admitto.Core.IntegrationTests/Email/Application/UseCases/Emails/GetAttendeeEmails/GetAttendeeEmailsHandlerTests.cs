using Amolenk.Admitto.Core.Email.Application.UseCases.Emails.GetAttendeeEmails;
using Amolenk.Admitto.Core.Email.Domain.Entities;
using Amolenk.Admitto.Core.Email.Domain.ValueObjects;
using Amolenk.Admitto.Core.Registrations.Contracts.ValueObjects;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Core.IntegrationTests.Email.Application.UseCases.Emails.GetAttendeeEmails;

[TestClass]
public sealed class GetAttendeeEmailsHandlerTests(TestContext testContext) : AspireIntegrationTestBase
{
    // Given a registration has two logged emails sent at different times
    // When the attendee's emails are queried
    // Then both are returned ordered with the most recently sent email first
    [TestMethod]
    public async ValueTask TwoEmails_ReturnsMostRecentFirst()
    {
        var teamId = TeamId.New();
        var eventId = TicketedEventId.New();
        var registrationId = RegistrationId.From(Guid.NewGuid());

        var olderSentAt = DateTimeOffset.UtcNow.AddDays(-3);
        var newerSentAt = DateTimeOffset.UtcNow.AddDays(-1);

        var older = EmailLog.Create(
            teamId,
            eventId,
            idempotencyKey: "key-old",
            recipient: EmailAddress.From("alice@example.com"),
            emailType: "Confirmation",
            subject: "Your registration",
            status: EmailLogStatus.Sent,
            sentAt: olderSentAt,
            statusUpdatedAt: olderSentAt,
            registrationId: registrationId);

        var newer = EmailLog.Create(
            teamId,
            eventId,
            idempotencyKey: "key-new",
            recipient: EmailAddress.From("alice@example.com"),
            emailType: "Reminder",
            subject: "Upcoming event reminder",
            status: EmailLogStatus.Delivered,
            sentAt: newerSentAt,
            statusUpdatedAt: newerSentAt,
            registrationId: registrationId);

        await Environment.EmailDatabase.SeedAsync(db =>
        {
            db.EmailLog.Add(older);
            db.EmailLog.Add(newer);
        });

        var result = await NewHandler().HandleAsync(
            new GetAttendeeEmailsQuery(teamId, eventId, registrationId),
            testContext.CancellationToken);

        result.Count.ShouldBe(2);
        result[0].Subject.ShouldBe("Upcoming event reminder");
        result[1].Subject.ShouldBe("Your registration");
        result[0].Status.ShouldBe(EmailLogStatus.Delivered.ToString());
    }

    // Given a registration has no logged emails
    // When the attendee's emails are queried
    // Then an empty list is returned
    [TestMethod]
    public async ValueTask NoEmailsForRegistration_ReturnsEmptyList()
    {
        var result = await NewHandler().HandleAsync(
            new GetAttendeeEmailsQuery(TeamId.New(), TicketedEventId.New(), RegistrationId.From(Guid.NewGuid())),
            testContext.CancellationToken);

        result.ShouldBeEmpty();
    }

    // Given a registration has an email logged for this event and another for a different event
    // When the attendee's emails are queried for this event
    // Then only the email for this event is returned
    [TestMethod]
    public async ValueTask EmailsForDifferentEvent_ExcludedFromResults()
    {
        var teamId = TeamId.New();
        var eventId = TicketedEventId.New();
        var otherEventId = TicketedEventId.New();
        var registrationId = RegistrationId.From(Guid.NewGuid());
        var now = DateTimeOffset.UtcNow;

        var forThisEvent = 
EmailLog.Create(
            teamId, eventId, "key-1", EmailAddress.From("alice@example.com"), "Confirmation", "Your registration",
            EmailLogStatus.Sent, now, now, registrationId: registrationId);

        var forOtherEvent = 
EmailLog.Create(
            teamId, otherEventId, "key-2", EmailAddress.From("alice@example.com"), "Confirmation", "Other event",
            EmailLogStatus.Sent, now, now, registrationId: registrationId);

        await Environment.EmailDatabase.SeedAsync(db =>
        {
            db.EmailLog.Add(forThisEvent);
            db.EmailLog.Add(forOtherEvent);
        });

        var result = await NewHandler().HandleAsync(
            new GetAttendeeEmailsQuery(teamId, eventId, registrationId),
            testContext.CancellationToken);

        result.ShouldHaveSingleItem().Subject.ShouldBe("Your registration");
    }

    // Given emails logged for this registration and for a different registration in the same event
    // When the attendee's emails are queried for this registration
    // Then only the email for this registration is returned
    [TestMethod]
    public async ValueTask EmailsForDifferentRegistration_ExcludedFromResults()
    {
        var teamId = TeamId.New();
        var eventId = TicketedEventId.New();
        var registrationId = RegistrationId.From(Guid.NewGuid());
        var otherRegistrationId = RegistrationId.From(Guid.NewGuid());
        var now = DateTimeOffset.UtcNow;

        var forThisRegistration = 
EmailLog.Create(
            teamId, eventId, "key-1", EmailAddress.From("alice@example.com"), "Confirmation", "Alice's confirmation",
            EmailLogStatus.Sent, now, now, registrationId: registrationId);

        var forOtherRegistration = 
EmailLog.Create(
            teamId, eventId, "key-2", EmailAddress.From("bob@example.com"), "Confirmation", "Bob's confirmation",
            EmailLogStatus.Sent, now, now, registrationId: otherRegistrationId);

        await Environment.EmailDatabase.SeedAsync(db =>
        {
            db.EmailLog.Add(forThisRegistration);
            db.EmailLog.Add(forOtherRegistration);
        });

        var result = await NewHandler().HandleAsync(
            new GetAttendeeEmailsQuery(teamId, eventId, registrationId),
            testContext.CancellationToken);

        result.ShouldHaveSingleItem().Subject.ShouldBe("Alice's confirmation");
    }

    // Given a logged email that has no associated registration id
    // When the attendee's emails are queried for a specific registration
    // Then that email is not included in the results
    [TestMethod]
    public async ValueTask EmailWithoutRegistrationId_NotIncluded()
    {
        var teamId = TeamId.New();
        var eventId = TicketedEventId.New();
        var registrationId = RegistrationId.From(Guid.NewGuid());
        var now = DateTimeOffset.UtcNow;

        var withoutRegistration = 
EmailLog.Create(
            teamId, eventId, "key-1", EmailAddress.From("alice@example.com"), "Confirmation", "Bulk email subject",
            EmailLogStatus.Sent, now, now, registrationId: null);

        await Environment.EmailDatabase.SeedAsync(db => db.EmailLog.Add(withoutRegistration));

        var result = await NewHandler().HandleAsync(
            new GetAttendeeEmailsQuery(teamId, eventId, registrationId),
            testContext.CancellationToken);

        result.ShouldBeEmpty();
    }

    // Given an email logged for a registration under team A
    // When the attendee's emails are queried using team B's id
    // Then no emails are returned
    [TestMethod]
    public async ValueTask EmailsForDifferentTeam_ExcludedFromResults()
    {
        var teamIdA = TeamId.New();
        var teamIdB = TeamId.New();
        var eventId = TicketedEventId.New();
        var registrationId = RegistrationId.From(Guid.NewGuid());
        var now = DateTimeOffset.UtcNow;

        var forTeamA =
EmailLog.Create(
            teamIdA, eventId, "key-cross-team", EmailAddress.From("alice@example.com"), "Confirmation", "Team A email",
            EmailLogStatus.Sent, now, now, registrationId: registrationId);

        await Environment.EmailDatabase.SeedAsync(db => db.EmailLog.Add(forTeamA));

        // Act: query with team B's ID
        var result = await NewHandler().HandleAsync(
            new GetAttendeeEmailsQuery(teamIdB, eventId, registrationId),
            testContext.CancellationToken);

        // Assert: cross-team access returns nothing
        result.ShouldBeEmpty();
    }

    private static GetAttendeeEmailsHandler NewHandler() =>
        new(Environment.EmailDatabase.Context);
}
