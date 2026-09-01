using Amolenk.Admitto.Core.Email.Application.Projections.EventEmailContext;
using Amolenk.Admitto.Core.Email.Application.Templating;
using Amolenk.Admitto.Core.Email.Application.Projections.TeamEmailContext;
using Amolenk.Admitto.Core.Email.Application.UseCases.Emails.PrepareEmailDelivery;
using Amolenk.Admitto.Core.Email.Application.UseCases.Emails.PrepareEmailDelivery.EventHandlers;
using Amolenk.Admitto.Core.Email.Application.UseCases.Emails.ComposeTransactionalEmail;
using Amolenk.Admitto.Core.Email.Domain.Entities;
using Amolenk.Admitto.Core.Email.Domain.ValueObjects;
using Amolenk.Admitto.Core.Registrations.Contracts.IntegrationEvents;
using Amolenk.Admitto.Core.Registrations.Contracts.ValueObjects;
using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Amolenk.Admitto.Core.Shared.Infrastructure.Persistence.Outbox;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Amolenk.Admitto.Core.IntegrationTests.Email.Application.UseCases.Emails.RequestTicketConfirmationResend;

[TestClass]
public sealed class RequestTicketConfirmationResendHandlerTests(TestContext testContext) : AspireIntegrationTestBase
{
    private static readonly TeamId TeamId = TeamId.New();
    private static readonly TicketedEventId EventId = TicketedEventId.New();
    private static readonly Guid RegistrationId = Guid.NewGuid();
    private static readonly Guid ResendRequestId = Guid.NewGuid();

    // Given the original ticket confirmation email was already logged as sent
    // When a ticket confirmation resend is requested
    // Then a new pending resend email log is created alongside the original
    [TestMethod]
    public async ValueTask HandleAsync_OriginalSentLogExists_CreatesResendLog()
    {
        await SeedOriginalSentLogAsync();
        var sut = await BuildHandlerAsync();

        await sut.HandleAsync(Command(), testContext.CancellationToken);
        await Environment.EmailDatabase.Context.SaveChangesAsync(testContext.CancellationToken);

        var logs = await Environment.EmailDatabase.Context.EmailLog
            .AsNoTracking()
            .OrderBy(l => l.IdempotencyKey)
            .ToListAsync(testContext.CancellationToken);

        logs.Count.ShouldBe(2);
        logs.ShouldContain(l => l.IdempotencyKey.StartsWith("attendee-registered:"));
        var resend = logs.Single(l => l.IdempotencyKey.StartsWith("ticket-confirmation-resend:"));
        resend.EmailType.ShouldBe(BuiltInEmailTemplateNames.TicketConfirmation);
        resend.Status.ShouldBe(EmailLogStatus.Pending);
        resend.RegistrationId.ShouldBe(Amolenk.Admitto.Core.Registrations.Contracts.ValueObjects.RegistrationId.From(RegistrationId));
    }

    // Given a ticket confirmation resend request
    // When the same resend request is handled twice
    // Then only one resend email log is created
    [TestMethod]
    public async ValueTask HandleAsync_SameResendRequestHandledTwice_CreatesOneResendLog()
    {
        var sut = await BuildHandlerAsync();
        var command = Command();

        await sut.HandleAsync(command, testContext.CancellationToken);
        await Environment.EmailDatabase.Context.SaveChangesAsync(testContext.CancellationToken);
        await sut.HandleAsync(command, testContext.CancellationToken);
        await Environment.EmailDatabase.Context.SaveChangesAsync(testContext.CancellationToken);

        var resendLogCount = await Environment.EmailDatabase.Context.EmailLog
            .AsNoTracking()
            .CountAsync(l => l.IdempotencyKey == $"ticket-confirmation-resend:{RegistrationId}:{ResendRequestId}", testContext.CancellationToken);

        resendLogCount.ShouldBe(1);
    }

    private async ValueTask<TicketConfirmationResendRequestedIntegrationEventHandler> BuildHandlerAsync()
    {
        await SeedTeamEmailContextAsync();

        return new TicketConfirmationResendRequestedIntegrationEventHandler(
            BuildComposer(),
            new PrepareEmailDeliveryHandler(Environment.EmailDatabase.Context, new Outbox(Environment.EmailDatabase.Context)));
    }

    private async ValueTask SeedTeamEmailContextAsync()
    {
        var now = DateTimeOffset.UtcNow;
        var teamContext = TeamEmailContextView.Create(
            TeamId, "DevConf Team", "#0f766e", teamVersion: 1, now);

        await Environment.EmailDatabase.SeedAsync(db => db.TeamEmailContexts.Add(teamContext));
        var eventContext = EventEmailContextView.CreatePartial(TeamId, EventId, now);
        eventContext.UpdateEventContext(1, "DevConf", "https://devconf.example.com", "devconf", "UTC", 1,
            null, false, now);
        await Environment.EmailDatabase.SeedAsync(db => db.EventEmailContexts.Add(eventContext));
    }

    private TransactionalEmailComposer BuildComposer()
    {
        return new TransactionalEmailComposer(
            Environment.EmailDatabase.Context,
            new ScribanEmailRenderer(),
            Options.Create(new PublicEventLinksOptions { BaseUrl = "https://tickets.example.com" }));
    }

    private static TicketConfirmationResendRequestedIntegrationEvent Command() =>
        new(
            TeamId.Value,
            EventId.Value,
            RegistrationId,
            ResendRequestId,
            "alice@example.com",
            "Alice",
            "Doe",
            ["General Admission"]);

    private async ValueTask SeedOriginalSentLogAsync()
    {
        var now = DateTimeOffset.UtcNow;
        await Environment.EmailDatabase.SeedAsync(db => db.EmailLog.Add(EmailLog.Create(
            teamId: TeamId,
            ticketedEventId: EventId,
            idempotencyKey: $"attendee-registered:{RegistrationId}:{now:O}",
            recipient: EmailAddress.From("alice@example.com"),
            emailType: BuiltInEmailTemplateNames.TicketConfirmation,
            subject: "Ticket confirmation",
            status: EmailLogStatus.Sent,
            sentAt: now,
            statusUpdatedAt: now,
            registrationId: Amolenk.Admitto.Core.Registrations.Contracts.ValueObjects.RegistrationId.From(RegistrationId))));
    }
}
