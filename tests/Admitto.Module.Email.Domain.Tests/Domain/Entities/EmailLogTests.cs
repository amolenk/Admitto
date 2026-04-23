using Amolenk.Admitto.Module.Email.Domain.Entities;
using Amolenk.Admitto.Module.Email.Domain.ValueObjects;
using Shouldly;

namespace Amolenk.Admitto.Module.Email.Domain.Tests.Entities;

[TestClass]
public sealed class EmailLogTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;

    [TestMethod]
    public void Create_SetsAllFields()
    {
        var teamId = Guid.NewGuid();
        var eventId = Guid.NewGuid();

        var log = EmailLog.Create(
            teamId,
            eventId,
            idempotencyKey: "key-1",
            recipient: "attendee@example.com",
            emailType: EmailTemplateType.Ticket,
            subject: "Your ticket",
            provider: "smtp",
            providerMessageId: "msg-123",
            status: EmailLogStatus.Sent,
            sentAt: Now,
            statusUpdatedAt: Now);

        log.Id.Value.ShouldNotBe(Guid.Empty);
        log.TeamId.ShouldBe(teamId);
        log.TicketedEventId.ShouldBe(eventId);
        log.IdempotencyKey.ShouldBe("key-1");
        log.Recipient.ShouldBe("attendee@example.com");
        log.EmailType.ShouldBe(EmailTemplateType.Ticket);
        log.Subject.ShouldBe("Your ticket");
        log.Provider.ShouldBe("smtp");
        log.ProviderMessageId.ShouldBe("msg-123");
        log.Status.ShouldBe(EmailLogStatus.Sent);
        log.SentAt.ShouldBe(Now);
        log.StatusUpdatedAt.ShouldBe(Now);
        log.LastError.ShouldBeNull();
    }

    [TestMethod]
    public void Create_WithNullOptionals_SetsNulls()
    {
        var log = EmailLog.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            idempotencyKey: "key-2",
            recipient: "attendee@example.com",
            emailType: EmailTemplateType.Ticket,
            subject: "Your ticket",
            provider: "smtp",
            providerMessageId: null,
            status: EmailLogStatus.Failed,
            sentAt: null,
            statusUpdatedAt: Now,
            lastError: "Connection refused");

        log.ProviderMessageId.ShouldBeNull();
        log.SentAt.ShouldBeNull();
        log.LastError.ShouldBe("Connection refused");
        log.Status.ShouldBe(EmailLogStatus.Failed);
    }

    [TestMethod]
    public void Create_TwoLogs_HaveDistinctIds()
    {
        var log1 = EmailLog.Create(Guid.NewGuid(), Guid.NewGuid(), "k1", "a@b.com", EmailTemplateType.Ticket, "S", "smtp", null, EmailLogStatus.Sent, Now, Now);
        var log2 = EmailLog.Create(Guid.NewGuid(), Guid.NewGuid(), "k2", "a@b.com", EmailTemplateType.Ticket, "S", "smtp", null, EmailLogStatus.Sent, Now, Now);

        log1.Id.ShouldNotBe(log2.Id);
    }
}
