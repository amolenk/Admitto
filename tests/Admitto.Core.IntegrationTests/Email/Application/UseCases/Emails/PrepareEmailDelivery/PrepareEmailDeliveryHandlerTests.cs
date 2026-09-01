using Amolenk.Admitto.Core.Email.Domain.Entities;
using Amolenk.Admitto.Core.Email.Application.Templating;
using Amolenk.Admitto.Core.Email.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace Amolenk.Admitto.Core.IntegrationTests.Email.Application.UseCases.Emails.PrepareEmailDelivery;

[TestClass]
public sealed class PrepareEmailDeliveryHandlerTests(TestContext testContext) : AspireIntegrationTestBase
{
    // Given an already-rendered email
    // When delivery preparation is handled
    // Then a pending claim and the unchanged delivery payload are persisted
    [TestMethod]
    public async ValueTask HandleAsync_RenderedEmail_CreatesPendingClaimAndDeliveryCommand()
    {
        var fixture = PrepareEmailDeliveryFixture.RenderedEmail();

        await fixture.BuildHandler(Environment).HandleAsync(
            fixture.Command(),
            testContext.CancellationToken);
        await Environment.EmailDatabase.Context.SaveChangesAsync(testContext.CancellationToken);

        var log = await Environment.EmailDatabase.Context.EmailLog
            .AsNoTracking()
            .SingleAsync(testContext.CancellationToken);
        log.Status.ShouldBe(EmailLogStatus.Pending);
        log.EmailType.ShouldBe(BuiltInEmailTemplateNames.TicketConfirmation);
        log.Subject.ShouldBe("Rendered subject");
        log.RegistrationId!.Value.Value.ShouldBe(fixture.RegistrationId);
        log.RegistrationCycleId!.Value.Value.ShouldBe(fixture.RegistrationCycleId);

        var outbox = await Environment.EmailDatabase.Context.OutboxMessages
            .AsNoTracking()
            .SingleAsync(testContext.CancellationToken);
        outbox.Type.ShouldBe("Email:Emails.DeliverEmail.DeliverEmailCommand");
        var payload = outbox.Payload.RootElement;
        var payloadNames = payload.EnumerateObject().Select(property => property.Name).ToHashSet();
        payloadNames.Count.ShouldBe(10);
        new[]
        {
            "teamId", "ticketedEventId", "recipientAddress", "recipientName", "emailType",
            "idempotencyKey", "subject", "textBody", "htmlBody", "commandId"
        }.All(payloadNames.Contains).ShouldBeTrue();
        payload.GetProperty("teamId").GetGuid().ShouldBe(fixture.TeamId.Value);
        payload.GetProperty("ticketedEventId").GetGuid().ShouldBe(fixture.EventId.Value);
        payload.GetProperty("recipientAddress").GetString().ShouldBe("alice@example.com");
        payload.GetProperty("recipientName").GetString().ShouldBe("Alice");
        payload.GetProperty("emailType").GetString().ShouldBe(BuiltInEmailTemplateNames.TicketConfirmation);
        payload.GetProperty("idempotencyKey").GetString().ShouldBe(fixture.IdempotencyKey);
        payload.GetProperty("subject").GetString().ShouldBe("Rendered subject");
        payload.GetProperty("textBody").GetString().ShouldBe("Rendered text");
        payload.GetProperty("htmlBody").GetString().ShouldBe("<p>Rendered html</p>");
        payload.GetProperty("commandId").GetGuid().ShouldNotBe(Guid.Empty);
    }

    // Given a terminal claim with the matching idempotency key
    // When delivery preparation is handled again
    // Then it performs no database or outbox work
    [TestMethod]
    public async ValueTask HandleAsync_TerminalClaimExists_IsIdempotentNoOp()
    {
        var fixture = PrepareEmailDeliveryFixture.RenderedEmail();
        await fixture.SeedClaimAsync(Environment, EmailLogStatus.Sent, DateTimeOffset.UtcNow);

        await fixture.BuildHandler(Environment).HandleAsync(
            fixture.Command(),
            testContext.CancellationToken);
        await Environment.EmailDatabase.Context.SaveChangesAsync(testContext.CancellationToken);

        (await Environment.EmailDatabase.Context.OutboxMessages.CountAsync(testContext.CancellationToken)).ShouldBe(0);
        (await Environment.EmailDatabase.Context.EmailLog.CountAsync(testContext.CancellationToken)).ShouldBe(1);
    }

    // Given a pending claim from an interrupted preparation flow
    // When delivery preparation is handled for recovery
    // Then the durable delivery command is queued without rendering or SMTP resolution
    [TestMethod]
    public async ValueTask HandleAsync_PendingClaimExists_QueuesRecoveryDelivery()
    {
        var fixture = PrepareEmailDeliveryFixture.RenderedEmail();
        await fixture.SeedClaimAsync(Environment, EmailLogStatus.Pending);

        await fixture.BuildHandler(Environment).HandleAsync(
            fixture.Command(),
            testContext.CancellationToken);
        await Environment.EmailDatabase.Context.SaveChangesAsync(testContext.CancellationToken);

        (await Environment.EmailDatabase.Context.OutboxMessages
            .CountAsync(m => m.Type == "Email:Emails.DeliverEmail.DeliverEmailCommand", testContext.CancellationToken))
            .ShouldBe(1);
    }

    // Given an already-rendered email without registration metadata
    // When delivery preparation is handled
    // Then the claim keeps both registration fields null
    [TestMethod]
    public async ValueTask HandleAsync_RegistrationMetadataOmitted_PersistsNullRegistrationFields()
    {
        var fixture = PrepareEmailDeliveryFixture.RenderedEmail();

        await fixture.BuildHandler(Environment).HandleAsync(
            fixture.CommandWithoutRegistrationMetadata(),
            testContext.CancellationToken);
        await Environment.EmailDatabase.Context.SaveChangesAsync(testContext.CancellationToken);

        var log = await Environment.EmailDatabase.Context.EmailLog
            .AsNoTracking()
            .SingleAsync(testContext.CancellationToken);
        log.RegistrationId.ShouldBeNull();
        log.RegistrationCycleId.ShouldBeNull();
    }

}
