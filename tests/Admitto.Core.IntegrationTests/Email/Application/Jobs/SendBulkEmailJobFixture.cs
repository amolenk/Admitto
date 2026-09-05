using Amolenk.Admitto.Core.Email.Application.Jobs;
using Amolenk.Admitto.Core.Email.Application.Persistence;
using Amolenk.Admitto.Core.Email.Application.Projections.EventEmailContext;
using Amolenk.Admitto.Core.Email.Application.Projections.TeamEmailContext;
using Amolenk.Admitto.Core.Email.Application.Sending;
using Amolenk.Admitto.Core.Email.Application.Sending.Bulk;
using Amolenk.Admitto.Core.Email.Application.Sending.Settings;
using Amolenk.Admitto.Core.Email.Application.Templating;
using Amolenk.Admitto.Core.Email.Application.UseCases.EventEmailContexts.GetEventEmailRenderingContext;
using Amolenk.Admitto.Core.Email.Domain.Entities;
using Amolenk.Admitto.Core.Email.Domain.ValueObjects;
using Amolenk.Admitto.Core.Email.Infrastructure.Persistence;
using Amolenk.Admitto.Core.IntegrationTests.Email.Application.Jobs.Fakes;
using Amolenk.Admitto.Core.Registrations.Contracts;
using Amolenk.Admitto.Core.Registrations.Contracts.ValueObjects;
using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Amolenk.Admitto.Core.Shared.Application.Persistence;
using Amolenk.Admitto.Core.Shared.Infrastructure.Messaging;
using Amolenk.Admitto.Core.Shared.Infrastructure.Persistence;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;
using Amolenk.Admitto.Testing.Builders.Email.Application;
using Amolenk.Admitto.Testing.Builders.Email.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using Quartz;

namespace Amolenk.Admitto.Core.IntegrationTests.Email.Application.Jobs;

internal sealed class SendBulkEmailJobFixture
{
    private readonly IReadOnlyList<BulkEmailRecipient> _recipients;
    private readonly string _emailType;
    private readonly string? _teamName;
    private readonly string? _subject;
    private readonly string? _textBody;
    private readonly string? _htmlBody;
    private readonly int? _inlineRetryCount;
    private readonly IRegistrationsFacade? _registrationsFacade;
    private readonly TimeProvider _timeProvider;

    private SendBulkEmailJobFixture(
        IReadOnlyList<BulkEmailRecipient> recipients,
        string emailType,
        string? teamName = null,
        string? subject = null,
        string? textBody = null,
        string? htmlBody = null,
        int? inlineRetryCount = null,
        IRegistrationsFacade? registrationsFacade = null,
        TimeProvider? timeProvider = null)
    {
        _recipients = recipients;
        _emailType = emailType;
        _teamName = teamName;
        _subject = subject;
        _textBody = textBody;
        _htmlBody = htmlBody;
        _inlineRetryCount = inlineRetryCount;
        _registrationsFacade = registrationsFacade;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    public static SendBulkEmailJobFixture Standard(IReadOnlyList<BulkEmailRecipient> recipients) =>
        new(recipients, BuiltInEmailTemplateNames.Reconfirmation);

    public static SendBulkEmailJobFixture PlatformSender(
        IReadOnlyList<BulkEmailRecipient> recipients,
        string teamName) =>
        new(recipients, BuiltInEmailTemplateNames.Reconfirmation, teamName: teamName);

    public static SendBulkEmailJobFixture CustomContent(
        IReadOnlyList<BulkEmailRecipient> recipients,
        string emailType,
        string subject,
        string textBody,
        string htmlBody) =>
        new(recipients, emailType, subject: subject, textBody: textBody, htmlBody: htmlBody);

    public static SendBulkEmailJobFixture ReconfirmWithFacade(
        IReadOnlyList<BulkEmailRecipient> recipients,
        IRegistrationsFacade registrationsFacade) =>
        new(recipients, BuiltInEmailTemplateNames.Reconfirmation, registrationsFacade: registrationsFacade);

    public static SendBulkEmailJobFixture ReconfirmWithFacadeAt(
        IReadOnlyList<BulkEmailRecipient> recipients,
        IRegistrationsFacade registrationsFacade,
        TimeProvider timeProvider) =>
        new(recipients, BuiltInEmailTemplateNames.Reconfirmation,
            registrationsFacade: registrationsFacade, timeProvider: timeProvider);

    public static SendBulkEmailJobFixture Retryable(
        IReadOnlyList<BulkEmailRecipient> recipients,
        int inlineRetryCount) =>
        new(recipients, BuiltInEmailTemplateNames.Reconfirmation, inlineRetryCount: inlineRetryCount);

    public async ValueTask<(BulkEmailJob Job, FakeBulkSmtpSender Sender, SendBulkEmailJob FanOut)> SetupAsync(
        IntegrationTestEnvironment environment)
    {
        var teamId = TeamId.New();
        var eventId = TicketedEventId.New();
        var attendeeFilter = _emailType == BuiltInEmailTemplateNames.Reconfirmation && _recipients.Count > 0
            ? new BulkEmailAttendeeFilter(
                RegistrationIds: _recipients.Select(r => r.RegistrationId.Value).ToArray(),
                RegistrationCycleIds: _recipients
                    .Where(r => r.RegistrationCycleId is not null)
                    .ToDictionary(r => r.RegistrationId.Value, r => r.RegistrationCycleId!.Value.Value))
            : new BulkEmailAttendeeFilter();
        var job = new BulkEmailJobBuilder()
            .ForTeam(teamId)
            .ForEvent(eventId)
            .WithEmailType(_emailType)
            .WithAdHocBodies(_subject, _textBody, _htmlBody)
            .WithAttendeeFilter(attendeeFilter)
            .Build();

        await environment.EmailDatabase.SeedAsync(db =>
        {
            db.BulkEmailJobs.Add(job);
            db.TeamEmailContexts.Add(CreateTeamEmailContext(teamId, _teamName));
            db.EventEmailContexts.Add(CreateEventEmailContext(teamId, eventId, DateTimeOffset.UtcNow));
        });

        var sender = new FakeBulkSmtpSender();
        var resolver = Substitute.For<IBulkEmailRecipientResolver>();
        resolver.ResolveAsync(teamId, eventId, Arg.Any<BulkEmailAttendeeFilter>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(_recipients));
        var registrationsFacade = _registrationsFacade ?? CurrentRegistrationsFacade(_recipients);
        var fanOut = BuildFanOut(
            environment,
            sender,
            resolver,
            registrationsFacade,
            _timeProvider,
            _inlineRetryCount ?? new BulkEmailOptions().InlineRetryCount);
        return (job, sender, fanOut);
    }

    public static BulkEmailRecipient Recipient(
        string email,
        string? name = null,
        RegistrationCycleId? cycleId = null) =>
        BulkEmailJobBuilder.Recipient(email, name, cycleId);

    public static SendBulkEmailJob BuildExistingJobFanOut(
        IntegrationTestEnvironment environment,
        FakeBulkSmtpSender sender,
        IBulkEmailRecipientResolver recipientResolver,
        IRegistrationsFacade registrationsFacade) =>
        BuildFanOut(environment, sender, recipientResolver, registrationsFacade, TimeProvider.System,
            new BulkEmailOptions().InlineRetryCount);

    public static SendBulkEmailJob BuildExistingJobFanOutAt(
        IntegrationTestEnvironment environment,
        FakeBulkSmtpSender sender,
        IBulkEmailRecipientResolver recipientResolver,
        IRegistrationsFacade registrationsFacade,
        TimeProvider timeProvider) =>
        BuildFanOut(environment, sender, recipientResolver, registrationsFacade, timeProvider,
            new BulkEmailOptions().InlineRetryCount);

    public static SendBulkEmailJob BuildLegacyFanOut(
        IntegrationTestEnvironment environment,
        FakeBulkSmtpSender sender,
        IBulkEmailRecipientResolver recipientResolver) =>
        BuildFanOut(
            environment,
            sender,
            recipientResolver,
            Substitute.For<IRegistrationsFacade>(),
            TimeProvider.System,
            new BulkEmailOptions().InlineRetryCount);

    private static SendBulkEmailJob BuildFanOut(
        IntegrationTestEnvironment environment,
        FakeBulkSmtpSender sender,
        IBulkEmailRecipientResolver recipientResolver,
        IRegistrationsFacade registrationsFacade,
        TimeProvider timeProvider,
        int inlineRetryCount)
    {
        var ctx = environment.EmailDatabase.Context;
        IEmailWriteStore writeStore = ctx;
        var settingsResolver = new EffectiveEmailSettingsResolver(Options.Create(new SystemEmailOptions
        {
            SmtpHost = "smtp.example.com",
            SmtpPort = 587,
            FromAddress = "tickets@admitto.org",
            AuthMode = "None"
        }), ctx);
        var eventContextQuery = Substitute.For<IQueryHandler<GetEventEmailRenderingContextQuery, EventEmailContextDto>>();
        eventContextQuery.HandleAsync(Arg.Any<GetEventEmailRenderingContextQuery>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                var query = call.Arg<GetEventEmailRenderingContextQuery>()!;
                return new EventEmailContextDto(
                    query.TeamId.Value,
                    query.TicketedEventId.Value,
                    "DevConf Team",
                    "DevConf",
                    "https://example.com",
                    "https://tickets.example.com/e/devconf",
                    "https://tickets.example.com/e/devconf/register",
                    "https://tickets.example.com/e/devconf/qr-code",
                    "https://tickets.example.com/e/devconf/cancel",
                    "https://tickets.example.com/e/devconf/edit",
                    "UTC",
                    null,
                    null,
                    null,
                    false);
            });
        var options = new BulkEmailOptions
        {
            PerMessageDelay = TimeSpan.Zero,
            InlineRetryCount = inlineRetryCount,
            InlineRetryDelay = TimeSpan.Zero
        };
        return new SendBulkEmailJob(
            writeStore,
            recipientResolver,
            registrationsFacade,
            eventContextQuery,
            settingsResolver,
            new EmailTemplateService(),
            new ScribanEmailRenderer(),
            sender,
            new UnitOfWork<EmailDbContext>(ctx, new NoOpOutboxMessageSender(), NullLogger<UnitOfWork<EmailDbContext>>.Instance),
            new StaticOptionsMonitor<BulkEmailOptions>(options),
            NullLogger<SendBulkEmailJob>.Instance,
            timeProvider);
    }

    public static IJobExecutionContext JobContext(BulkEmailJob job)
    {
        var data = new JobDataMap
        {
            [SendBulkEmailJob.BulkEmailJobIdKey] = job.Id.Value.ToString(),
            [SendBulkEmailJob.TeamIdKey] = job.TeamId.Value.ToString(),
            [SendBulkEmailJob.TicketedEventIdKey] = job.TicketedEventId.Value.ToString()
        };
        var context = Substitute.For<IJobExecutionContext>();
        context.MergedJobDataMap.Returns(data);
        context.CancellationToken.Returns(CancellationToken.None);
        return context;
    }

    public static TeamEmailContextView CreateTeamEmailContext(TeamId teamId, string? teamName = null) =>
        TeamEmailContextView.Create(teamId, teamName ?? "DevConf Team", "#0f766e", teamVersion: 1, DateTimeOffset.UtcNow);

    public static EventEmailContextView CreateEventEmailContext(
        TeamId teamId,
        TicketedEventId eventId,
        DateTimeOffset now) =>
        new EventEmailContextViewBuilder()
            .ForTeam(teamId)
            .ForEvent(eventId)
            .At(now)
            .WithWindow(now.AddHours(-1), now.AddHours(1))
            .WithMinEmailIntervalHours(1)
            .Build();

    public static IBulkEmailRecipientResolver NeverCalledResolver()
    {
        var resolver = Substitute.For<IBulkEmailRecipientResolver>();
        resolver.WhenForAnyArgs(r => r.ResolveAsync(TeamId.New(), TicketedEventId.New(), default!, default))
            .Do(_ => throw new InvalidOperationException("Resolver should not be called when resuming an in-flight job."));
        return resolver;
    }

    public static IRegistrationsFacade CurrentRegistrationsFacade(
        IReadOnlyList<BulkEmailRecipient> recipients)
    {
        var facade = Substitute.For<IRegistrationsFacade>();
        facade.GetRegistrationsAsync(
                Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<QueryRegistrationsDto>(), Arg.Any<CancellationToken>())
            .Returns(recipients.Select(recipient => new RegistrationListItemDto(
                recipient.RegistrationId.Value,
                recipient.Email.Value,
                recipient.DisplayName,
                string.Empty,
                [],
                new Dictionary<string, string>(),
                DateTimeOffset.UtcNow.AddDays(-1),
                recipient.RegistrationCycleId?.Value ?? Guid.NewGuid(),
                1,
                1,
                RegistrationStatus.Registered,
                false,
                null,
                null)).ToList());
        facade.GetReconfirmDeliveryStateAsync(
                Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<ReconfirmDeliveryQuery>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                var query = call.Arg<ReconfirmDeliveryQuery>()!;
                var recipient = recipients.FirstOrDefault(r => r.RegistrationId.Value == query.RegistrationId);
                return recipient is null
                    ? new ReconfirmDeliveryState.Suppressed(ReconfirmDeliverySuppression.RegistrationNotFound)
                    : new ReconfirmDeliveryState.Allowed(
                        DateTimeOffset.UtcNow.AddDays(-1),
                        TimeSpan.FromHours(1),
                        null,
                        DateTimeOffset.UtcNow.AddYears(10));
            });
        return facade;
    }

    public static RegistrationListItemDto CurrentRow(
        BulkEmailRecipient recipient,
        RegistrationCycleId cycleId,
        IReadOnlyCollection<Guid>? ticketTypeIds = null,
        int? maxReconfirmationEmails = null) =>
        new(
            recipient.RegistrationId.Value,
            recipient.Email.Value,
            "Alice",
            "Test",
            ticketTypeIds ?? [],
            new Dictionary<string, string>(),
            DateTimeOffset.UtcNow.AddDays(-1),
            cycleId.Value,
            1,
            1,
            RegistrationStatus.Registered,
            false,
            null,
            maxReconfirmationEmails);

    public static ReconfirmDeliveryState DeliveryState(
        TimeSpan? minimumInterval = null,
        int? maximum = null,
        ReconfirmDeliverySuppression? suppression = null,
        DateTimeOffset? cutoffAt = null) =>
        suppression is not null
            ? new ReconfirmDeliveryState.Suppressed(suppression.Value)
            : new ReconfirmDeliveryState.Allowed(
                DateTimeOffset.UtcNow.AddDays(-1),
                minimumInterval ?? TimeSpan.FromHours(1),
                maximum,
                cutoffAt ?? DateTimeOffset.UtcNow.AddYears(10));

    private sealed class StaticOptionsMonitor<T>(T value) : IOptionsMonitor<T>
    {
        public T CurrentValue => value;
        public T Get(string? name) => value;
        public IDisposable? OnChange(Action<T, string?> listener) => null;
    }
}
