using Amolenk.Admitto.Core.Email.Application.UseCases.AttendeeEmails.GetAttendeeEmails;
using Amolenk.Admitto.Core.Email.Domain.Entities;
using Amolenk.Admitto.Core.Email.Domain.ValueObjects;

namespace Amolenk.Admitto.Core.IntegrationTests.Email.Application.UseCases.AttendeeEmails.GetAttendeeEmails;

[TestClass]
public sealed class GetAttendeeEmailsHandlerTests(TestContext testContext) : AspireIntegrationTestBase
{
    [TestMethod]
    public async ValueTask TwoEmails_ReturnsMostRecentFirst()
    {
        var teamId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        var registrationId = Guid.NewGuid();

        var olderSentAt = DateTimeOffset.UtcNow.AddDays(-3);
        var newerSentAt = DateTimeOffset.UtcNow.AddDays(-1);

        var older = EmailLog.Create(
            teamId,
            eventId,
            idempotencyKey: "key-old",
            recipient: "alice@example.com",
            emailType: "Confirmation",
            subject: "Your registration",
            provider: "test",
            providerMessageId: null,
            status: EmailLogStatus.Sent,
            sentAt: olderSentAt,
            statusUpdatedAt: olderSentAt,
            registrationId: registrationId);

        var newer = EmailLog.Create(
            teamId,
            eventId,
            idempotencyKey: "key-new",
            recipient: "alice@example.com",
            emailType: "Reminder",
            subject: "Upcoming event reminder",
            provider: "test",
            providerMessageId: null,
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

    [TestMethod]
    public async ValueTask NoEmailsForRegistration_ReturnsEmptyList()
    {
        var result = await NewHandler().HandleAsync(
            new GetAttendeeEmailsQuery(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()),
            testContext.CancellationToken);

        result.ShouldBeEmpty();
    }

    [TestMethod]
    public async ValueTask EmailsForDifferentEvent_ExcludedFromResults()
    {
        var teamId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        var otherEventId = Guid.NewGuid();
        var registrationId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        var forThisEvent = EmailLog.Create(
            teamId, eventId, "key-1", "alice@example.com", "Confirmation", "Your registration",
            "test", null, EmailLogStatus.Sent, now, now, registrationId: registrationId);

        var forOtherEvent = EmailLog.Create(
            teamId, otherEventId, "key-2", "alice@example.com", "Confirmation", "Other event",
            "test", null, EmailLogStatus.Sent, now, now, registrationId: registrationId);

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

    [TestMethod]
    public async ValueTask EmailsForDifferentRegistration_ExcludedFromResults()
    {
        var teamId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        var registrationId = Guid.NewGuid();
        var otherRegistrationId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        var forThisRegistration = EmailLog.Create(
            teamId, eventId, "key-1", "alice@example.com", "Confirmation", "Alice's confirmation",
            "test", null, EmailLogStatus.Sent, now, now, registrationId: registrationId);

        var forOtherRegistration = EmailLog.Create(
            teamId, eventId, "key-2", "bob@example.com", "Confirmation", "Bob's confirmation",
            "test", null, EmailLogStatus.Sent, now, now, registrationId: otherRegistrationId);

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

    [TestMethod]
    public async ValueTask EmailWithoutRegistrationId_NotIncluded()
    {
        var teamId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        var registrationId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        var withoutRegistration = EmailLog.Create(
            teamId, eventId, "key-1", "alice@example.com", "Confirmation", "Bulk email subject",
            "test", null, EmailLogStatus.Sent, now, now, registrationId: null);

        await Environment.EmailDatabase.SeedAsync(db => db.EmailLog.Add(withoutRegistration));

        var result = await NewHandler().HandleAsync(
            new GetAttendeeEmailsQuery(teamId, eventId, registrationId),
            testContext.CancellationToken);

        result.ShouldBeEmpty();
    }

    private static GetAttendeeEmailsHandler NewHandler() =>
        new(Environment.EmailDatabase.Context);
}
