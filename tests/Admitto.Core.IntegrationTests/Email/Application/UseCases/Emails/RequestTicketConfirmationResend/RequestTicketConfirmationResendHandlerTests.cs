using Amolenk.Admitto.Core.Email.Application.Sending;
using Amolenk.Admitto.Core.Email.Application.Sending.Settings;
using Amolenk.Admitto.Core.Email.Application.Templating;
using Amolenk.Admitto.Core.Email.Application.Projections.TeamEmailContext;
using Amolenk.Admitto.Core.Email.Application.UseCases.Emails.SendEmail;
using Amolenk.Admitto.Core.Email.Application.UseCases.Emails.SendEmail.EventHandlers;
using Amolenk.Admitto.Core.Email.Application.UseCases.EventEmailContexts.GetEventEmailRenderingContext;
using Amolenk.Admitto.Core.Email.Domain.Entities;
using Amolenk.Admitto.Core.Email.Domain.ValueObjects;
using Amolenk.Admitto.Core.Registrations.Contracts.IntegrationEvents;
using Amolenk.Admitto.Core.Registrations.Contracts.ValueObjects;
using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Amolenk.Admitto.Core.Shared.Infrastructure.Persistence.Outbox;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace Amolenk.Admitto.Core.IntegrationTests.Email.Application.UseCases.Emails.RequestTicketConfirmationResend;

[TestClass]
public sealed class RequestTicketConfirmationResendHandlerTests(TestContext testContext) : AspireIntegrationTestBase
{
    private static readonly TeamId TeamId = TeamId.New();
    private static readonly TicketedEventId EventId = TicketedEventId.New();
    private static readonly Guid RegistrationId = Guid.NewGuid();
    private static readonly Guid ResendRequestId = Guid.NewGuid();

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

        var eventContextQuery = Substitute.For<IQueryHandler<GetEventEmailRenderingContextQuery, EventEmailContextDto>>();
        eventContextQuery
            .HandleAsync(Arg.Any<GetEventEmailRenderingContextQuery>(), Arg.Any<CancellationToken>())
            .Returns(new EventEmailContextDto(
                TeamId.Value,
                EventId.Value,
                "DevConf Team",
                "DevConf",
                "https://devconf.example.com",
                "https://tickets.example.com/devconf",
                "https://tickets.example.com/devconf/register",
                $"https://tickets.example.com/devconf/qr-code/{RegistrationId}",
                $"https://tickets.example.com/devconf/cancel/{RegistrationId}",
                "#0f766e",
                $"https://tickets.example.com/devconf/edit/{RegistrationId}",
                "UTC",
                null,
                null,
                null,
                null,
                false));

        return new TicketConfirmationResendRequestedIntegrationEventHandler(eventContextQuery, BuildSendEmailHandler());
    }

    private async ValueTask SeedTeamEmailContextAsync()
    {
        var now = DateTimeOffset.UtcNow;
        var teamContext = TeamEmailContextView.CreatePartial(TeamId, now);
        teamContext.UpdateTeamContext("DevConf Team", "#0f766e", null, teamVersion: 1, now);

        await Environment.EmailDatabase.SeedAsync(db => db.TeamEmailContexts.Add(teamContext));
    }

    private SendEmailHandler BuildSendEmailHandler() =>
        new(
            Environment.EmailDatabase.Context,
            new EffectiveEmailSettingsResolver(
                Options.Create(new SystemEmailOptions
                {
                    SmtpHost = "smtp.example.com",
                    SmtpPort = 587,
                    FromAddress = "tickets@admitto.org",
                    AuthMode = "None"
                }),
                Environment.EmailDatabase.Context),
            new EmailTemplateService(),
            new ScribanEmailRenderer(),
            new Outbox(Environment.EmailDatabase.Context));

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
