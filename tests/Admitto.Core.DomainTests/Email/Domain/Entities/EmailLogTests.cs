using Amolenk.Admitto.Core.Email.Application.Templating;
using Amolenk.Admitto.Core.Email.Domain.Entities;
using Amolenk.Admitto.Core.Email.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;
using Shouldly;

namespace Amolenk.Admitto.Core.Email.Domain.Tests.Entities;

[TestClass]
public sealed class EmailLogTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;

    // When an email log is created with all fields specified
    // Then every property is set to the given value and LastError is null
    [TestMethod]
    public void Create_SetsAllFields()
    {
        var teamId = TeamId.New();
        var eventId = TicketedEventId.New();

        var log = EmailLog.Create(
            teamId,
            eventId,
            idempotencyKey: "key-1",
            recipient: EmailAddress.From("attendee@example.com"),
            emailType: BuiltInEmailTemplateNames.TicketConfirmation,
            subject: "Your ticket",
            status: EmailLogStatus.Sent,
            sentAt: Now,
            statusUpdatedAt: Now);

        log.Id.Value.ShouldNotBe(Guid.Empty);
        log.TeamId.ShouldBe(teamId);
        log.TicketedEventId.ShouldBe(eventId);
        log.IdempotencyKey.ShouldBe("key-1");
        log.Recipient.ShouldBe(EmailAddress.From("attendee@example.com"));
        log.EmailType.ShouldBe(BuiltInEmailTemplateNames.TicketConfirmation);
        log.Subject.ShouldBe("Your ticket");
        log.Status.ShouldBe(EmailLogStatus.Sent);
        log.SentAt.ShouldBe(Now);
        log.StatusUpdatedAt.ShouldBe(Now);
        log.LastError.ShouldBeNull();
    }

    // Given a failed send with no sent-at timestamp but a last error message
    // When the email log is created
    // Then SentAt is null, LastError holds the error message, and Status is Failed
    [TestMethod]
    public void Create_WithNullOptionals_SetsNulls()
    {
        var log = EmailLog.Create(
            TeamId.New(),
            TicketedEventId.New(),
            idempotencyKey: "key-2",
            recipient: EmailAddress.From("attendee@example.com"),
            emailType: BuiltInEmailTemplateNames.TicketConfirmation,
            subject: "Your ticket",
            status: EmailLogStatus.Failed,
            sentAt: null,
            statusUpdatedAt: Now,
            lastError: "Connection refused");

        log.SentAt.ShouldBeNull();
        log.LastError.ShouldBe("Connection refused");
        log.Status.ShouldBe(EmailLogStatus.Failed);
    }

    // When two email logs are created
    // Then they are assigned distinct ids
    [TestMethod]
    public void Create_TwoLogs_HaveDistinctIds()
    {
        var log1 = EmailLog.Create(TeamId.New(), TicketedEventId.New(), "k1", EmailAddress.From("a@b.com"), BuiltInEmailTemplateNames.TicketConfirmation, "S", EmailLogStatus.Sent, Now, Now);
        var log2 = EmailLog.Create(TeamId.New(), TicketedEventId.New(), "k2", EmailAddress.From("a@b.com"), BuiltInEmailTemplateNames.TicketConfirmation, "S", EmailLogStatus.Sent, Now, Now);

        log1.Id.ShouldNotBe(log2.Id);
    }
}
