using Amolenk.Admitto.Core.Email.Application.Sending;
using Amolenk.Admitto.Core.Email.Domain.Entities;
using Amolenk.Admitto.Core.Email.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace Amolenk.Admitto.Core.IntegrationTests.Email.Application.UseCases.Emails.DeliverEmail;

[TestClass]
public sealed class DeliverEmailHandlerTests(TestContext testContext) : AspireIntegrationTestBase
{
    // Given an email log already marked Sent
    // When durable delivery is handled
    // Then no additional SMTP attempt is made
    [TestMethod]
    public async ValueTask HandleAsync_TerminalClaimExists_DoesNotSendAgain()
    {
        var fixture = DeliverEmailFixture.ValidTransport();
        await fixture.SeedClaimAsync(Environment, EmailLogStatus.Sent, DateTimeOffset.UtcNow);

        await fixture.BuildHandler(Environment).HandleAsync(
            fixture.Command(),
            testContext.CancellationToken);

        fixture.Sender.SendAttempts.ShouldBe(0);
    }

    // Given a pending email claim and valid SMTP transport
    // When durable delivery is handled
    // Then the message is sent and the claim is marked Sent
    [TestMethod]
    public async ValueTask HandleAsync_PendingClaimExists_SendsAndMarksSent()
    {
        var fixture = DeliverEmailFixture.ValidTransport();
        await fixture.SeedClaimAsync(Environment);

        await fixture.BuildHandler(Environment).HandleAsync(
            fixture.Command(),
            testContext.CancellationToken);
        await Environment.EmailDatabase.Context.SaveChangesAsync(testContext.CancellationToken);

        fixture.Sender.SendAttempts.ShouldBe(1);
        var log = await Environment.EmailDatabase.Context.EmailLog
            .AsNoTracking()
            .SingleAsync(testContext.CancellationToken);
        log.Status.ShouldBe(EmailLogStatus.Sent);
    }

    // Given a pending email claim and incomplete Basic credentials
    // When durable delivery is handled
    // Then the claim is marked Failed without attempting SMTP
    [TestMethod]
    public async ValueTask HandleAsync_InvalidBasicCredentials_MarksClaimFailedWithoutSending()
    {
        var fixture = DeliverEmailFixture.InvalidBasicCredentials();
        await fixture.SeedClaimAsync(Environment);

        await fixture.BuildHandler(Environment).HandleAsync(
            fixture.Command(),
            testContext.CancellationToken);
        await Environment.EmailDatabase.Context.SaveChangesAsync(testContext.CancellationToken);

        await AssertFailedWithoutSendingAsync(fixture);
    }

    // Given a pending email claim with conflicting SSL and STARTTLS settings
    // When durable delivery is handled
    // Then the claim is marked Failed without attempting SMTP
    [TestMethod]
    public async ValueTask HandleAsync_SslAndStartTlsConflict_MarksClaimFailedWithoutSending()
    {
        var fixture = DeliverEmailFixture.InvalidSecurityCombination();
        await fixture.SeedClaimAsync(Environment);

        await fixture.BuildHandler(Environment).HandleAsync(
            fixture.Command(),
            testContext.CancellationToken);
        await Environment.EmailDatabase.Context.SaveChangesAsync(testContext.CancellationToken);

        await AssertFailedWithoutSendingAsync(fixture);
    }

    // Given a pending email claim and absent SMTP host and sender address
    // When durable delivery is handled
    // Then the claim is marked Failed without attempting SMTP
    [TestMethod]
    public async ValueTask HandleAsync_MissingSmtpHostAndFromAddress_MarksClaimFailedWithoutSending()
    {
        var fixture = DeliverEmailFixture.MissingHostAndFrom();
        await fixture.SeedClaimAsync(Environment);

        await fixture.BuildHandler(Environment).HandleAsync(
            fixture.Command(),
            testContext.CancellationToken);
        await Environment.EmailDatabase.Context.SaveChangesAsync(testContext.CancellationToken);

        await AssertFailedWithoutSendingAsync(fixture);
    }

    // Given a pending claim and a sender that fails transiently
    // When durable delivery is handled
    // Then inline retries occur and the delivery command is requeued
    [TestMethod]
    public async ValueTask HandleAsync_TransientSmtpFailure_RetriesAndRequeues()
    {
        var fixture = DeliverEmailFixture.ValidTransportWithOptions(new EmailDeliveryOptions
            {
                InlineRetryCount = 2,
                InlineRetryDelay = TimeSpan.Zero,
                MaxDeliveryAttempts = 3
            });
        fixture.Sender.ShouldThrow = true;
        await fixture.SeedClaimAsync(Environment);

        await fixture.BuildHandler(Environment).HandleAsync(
            fixture.Command(),
            testContext.CancellationToken);
        await Environment.EmailDatabase.Context.SaveChangesAsync(testContext.CancellationToken);

        fixture.Sender.SendAttempts.ShouldBe(3);
        var log = await Environment.EmailDatabase.Context.EmailLog
            .AsNoTracking()
            .SingleAsync(testContext.CancellationToken);
        log.Status.ShouldBe(EmailLogStatus.Pending);
        log.DeliveryAttemptCount.ShouldBe(1);
        log.LastError.ShouldBe("SMTP error (fake)");
        (await Environment.EmailDatabase.Context.OutboxMessages.AnyAsync(
            message => message.Type == "Email:Emails.DeliverEmail.DeliverEmailCommand",
            testContext.CancellationToken)).ShouldBeTrue();
    }

    private async ValueTask AssertFailedWithoutSendingAsync(DeliverEmailFixture fixture)
    {
        fixture.Sender.SendAttempts.ShouldBe(0);
        var log = await Environment.EmailDatabase.Context.EmailLog
            .AsNoTracking()
            .SingleAsync(testContext.CancellationToken);
        log.Status.ShouldBe(EmailLogStatus.Failed);
        log.LastError.ShouldNotBeNullOrEmpty();
    }

}
