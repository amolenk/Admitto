using System.Text.Json;
using Amolenk.Admitto.Core.Email;
using Amolenk.Admitto.Core.Email.Application.Jobs;
using Amolenk.Admitto.Core.Email.Application.Persistence;
using Amolenk.Admitto.Core.Email.Application.Templating;
using Amolenk.Admitto.Core.Email.Contracts.IntegrationEvents;
using Amolenk.Admitto.Core.Email.Domain.Entities;
using Amolenk.Admitto.Core.Email.Domain.ValueObjects;
using Amolenk.Admitto.Core.Email.Infrastructure.Persistence;
using Amolenk.Admitto.Core.IntegrationTests.Email.Application.Jobs.Fakes;
using Amolenk.Admitto.Core.Registrations.Contracts;
using Amolenk.Admitto.Core.Shared.Application.Persistence;
using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Amolenk.Admitto.Core.Shared.Infrastructure.Persistence;
using Amolenk.Admitto.Core.Shared.Infrastructure.Persistence.Outbox;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;
using Amolenk.Admitto.Testing.Builders.Email.Application;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using Quartz;

namespace Amolenk.Admitto.Core.IntegrationTests.Email.Application.Jobs;

[TestClass]
public sealed class RequestReconfirmationsJobTests : AspireIntegrationTestBase
{
    private static readonly TeamId TeamId = TeamId.New();

    // Given an active policy and an attendee who registered too recently
    // When the stable hourly evaluator runs
    // Then no reconfirm email job is created
    [TestMethod]
    public async ValueTask Execute_AttendeeRegisteredRecently_ExcludedFromBulkJob()
    {
        var eventId = TicketedEventId.New();
        var now = DateTimeOffset.UtcNow;
        await SeedPolicyAsync(eventId, now);
        var facade = FacadeReturning(eventId, [RegistrationItem(Guid.NewGuid(), "alice@example.com", now.AddHours(-10))]);

        await BuildJob(facade, new FakeTimeProvider(now)).Execute(JobContext());

        (await LoadBulkEmailJobsAsync()).ShouldBeEmpty();
    }

    // Given an active policy and a sent reconfirm email inside the minimum interval
    // When the stable hourly evaluator runs
    // Then the attendee is not included in a new reconfirm email job
    [TestMethod]
    public async ValueTask Execute_AttendeeReceivedReconfirmRecently_ExcludedFromBulkJob()
    {
        var eventId = TicketedEventId.New();
        var now = DateTimeOffset.UtcNow;
        await SeedPolicyAsync(eventId, now);
        await Environment.EmailDatabase.SeedAsync(db =>
            db.EmailLog.Add(ReconfirmEmailLog(eventId, "alice@example.com", now.AddHours(-10))));
        var facade = FacadeReturning(eventId, [RegistrationItem(Guid.NewGuid(), "alice@example.com", now.AddHours(-72))]);

        await BuildJob(facade, new FakeTimeProvider(now)).Execute(JobContext());

        (await LoadBulkEmailJobsAsync()).ShouldBeEmpty();
    }

    // Given an active policy and an attendee whose interval has elapsed
    // When the stable hourly evaluator runs
    // Then one reconfirm email job is created for that attendee
    [TestMethod]
    public async ValueTask Execute_MinEmailIntervalElapsedSinceLastEmail_AttendeeIncluded()
    {
        var eventId = TicketedEventId.New();
        var now = DateTimeOffset.UtcNow;
        var attendeeId = Guid.NewGuid();
        await SeedPolicyAsync(eventId, now);
        await Environment.EmailDatabase.SeedAsync(db =>
            db.EmailLog.Add(ReconfirmEmailLog(eventId, "alice@example.com", now.AddHours(-72))));
        var facade = FacadeReturning(eventId, [RegistrationItem(attendeeId, "alice@example.com", now.AddHours(-100))]);

        await BuildJob(facade, new FakeTimeProvider(now)).Execute(JobContext());

        var jobs = await LoadBulkEmailJobsAsync();
        jobs.Count.ShouldBe(1);
        jobs[0].AttendeeFilter.RegistrationIds!.ShouldContain(attendeeId);
    }

    // Given an active reconfirm job already reserved for an event
    // When the hourly evaluator runs again
    // Then it does not create a second overlapping reconfirm job
    [TestMethod]
    public async ValueTask Execute_ActiveReconfirmJobExists_SkipsReservation()
    {
        var eventId = TicketedEventId.New();
        var now = DateTimeOffset.UtcNow;
        await SeedPolicyAsync(eventId, now);
        await Environment.EmailDatabase.SeedAsync(db => db.BulkEmailJobs.Add(
            BulkEmailJob.CreateSystemTriggered(
                TeamId,
                eventId,
                BuiltInEmailTemplateNames.Reconfirmation,
                null,
                null,
                null,
                new BulkEmailAttendeeFilter(
                    RegistrationStatus: RegistrationStatus.Registered,
                    HasReconfirmed: false),
                now)));
        var facade = FacadeReturning(eventId, [RegistrationItem(Guid.NewGuid(), "alice@example.com", now.AddDays(-2))]);

        await BuildJob(facade, new FakeTimeProvider(now)).Execute(JobContext());

        (await LoadBulkEmailJobsAsync()).Count.ShouldBe(1);
        await facade.DidNotReceiveWithAnyArgs().GetRegistrationsAsync(default, default, default!, default);
    }

    // Given archived, cleared, partial, future, and closed projected policies
    // When the hourly evaluator runs
    // Then none of those policies is evaluated
    [TestMethod]
    public async ValueTask Execute_NonEligibleProjectedPolicies_SkipsAll()
    {
        var now = DateTimeOffset.UtcNow;
        var archived = TicketedEventId.New();
        var cleared = TicketedEventId.New();
        var partial = TicketedEventId.New();
        var future = TicketedEventId.New();
        var closed = TicketedEventId.New();
        await SeedPolicyAsync(archived, now, archived: true);
        await SeedPolicyAsync(cleared, now, withoutPolicy: true);
        await SeedPolicyAsync(partial, now, withoutEventContext: true);
        await SeedPolicyAsync(future, now, opensAt: now.AddHours(1), closesAt: now.AddHours(2));
        await SeedPolicyAsync(closed, now, opensAt: now.AddHours(-2), closesAt: now);
        var facade = Substitute.For<IRegistrationsFacade>();

        await BuildJob(facade, new FakeTimeProvider(now)).Execute(JobContext());

        (await LoadBulkEmailJobsAsync()).ShouldBeEmpty();
        await facade.DidNotReceiveWithAnyArgs().GetRegistrationsAsync(default, default, default!, default);
    }

    // Given one policy opening now and another closing now
    // When the hourly evaluator runs at the boundary
    // Then only the open-inclusive policy is evaluated
    [TestMethod]
    public async ValueTask Execute_WindowBoundaries_UsesOpenInclusiveCloseExclusive()
    {
        var now = DateTimeOffset.UtcNow;
        var opensNow = TicketedEventId.New();
        var closesNow = TicketedEventId.New();
        await SeedPolicyAsync(opensNow, now, opensAt: now, closesAt: now.AddHours(1));
        await SeedPolicyAsync(closesNow, now, opensAt: now.AddHours(-1), closesAt: now);
        var facade = Substitute.For<IRegistrationsFacade>();
        facade.GetRegistrationsAsync(
                Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<QueryRegistrationsDto>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<RegistrationListItemDto>>([
                RegistrationItem(Guid.NewGuid(), "alice@example.com", now.AddDays(-2))]));

        await BuildJob(facade, new FakeTimeProvider(now)).Execute(JobContext());

        (await LoadBulkEmailJobsAsync()).Count.ShouldBe(1);
        await facade.Received(1).GetRegistrationsAsync(
            TeamId.Value,
            opensNow.Value,
            Arg.Any<QueryRegistrationsDto>(),
            Arg.Any<CancellationToken>());
    }

    // Given an active policy with an invalid projected timezone
    // When the hourly evaluator runs
    // Then the event is skipped rather than evaluated outside quiet hours
    [TestMethod]
    public async ValueTask Execute_InvalidProjectedTimezone_SkipsEvent()
    {
        var eventId = TicketedEventId.New();
        var now = DateTimeOffset.UtcNow;
        await SeedPolicyAsync(eventId, now, timeZone: "Not/AReal_Zone");
        var facade = FacadeReturning(eventId, [RegistrationItem(Guid.NewGuid(), "alice@example.com", now.AddDays(-2))]);

        await BuildJob(facade, new FakeTimeProvider(now)).Execute(JobContext());

        (await LoadBulkEmailJobsAsync()).ShouldBeEmpty();
        await facade.DidNotReceiveWithAnyArgs().GetRegistrationsAsync(default, default, default!, default);
    }

    // Given a maxed-out attendee and sent reconfirm attempts before the interval
    // When the hourly evaluator runs
    // Then it publishes automatic expiry without creating another email job
    [TestMethod]
    public async ValueTask Execute_MaxAttemptsReached_PublishesAutoExpiry()
    {
        var eventId = TicketedEventId.New();
        var now = DateTimeOffset.UtcNow;
        var registrationId = Guid.NewGuid();
        await SeedPolicyAsync(eventId, now);
        await Environment.EmailDatabase.SeedAsync(db =>
        {
            db.EmailLog.Add(ReconfirmEmailLog(eventId, "alice@example.com", now.AddDays(-4)));
            db.EmailLog.Add(ReconfirmEmailLog(eventId, "alice@example.com", now.AddDays(-3)));
        });
        var facade = FacadeReturning(eventId, [RegistrationItem(
            registrationId, "alice@example.com", now.AddDays(-10), effectiveMaxReconfirmAttempts: 2)]);

        await BuildJob(facade, new FakeTimeProvider(now)).Execute(JobContext());

        (await LoadBulkEmailJobsAsync()).ShouldBeEmpty();
        var outbox = await LoadOutboxMessagesAsync();
        outbox.Count.ShouldBe(1);
        GetRegistrationIds(outbox[0].Payload).ShouldBe([registrationId], ignoreOrder: true);
    }

    // Given an active policy with overnight quiet hours
    // When the hourly evaluation runs during the local quiet interval
    // Then no reconfirm email job is created
    [TestMethod]
    public async ValueTask Execute_DuringOvernightQuietHours_SkipsEvent()
    {
        var eventId = TicketedEventId.New();
        var now = new DateTimeOffset(2030, 6, 1, 23, 0, 0, TimeSpan.Zero);
        await SeedPolicyAsync(eventId, now, new TimeOnly(22), new TimeOnly(8));
        var facade = FacadeReturning(eventId, [RegistrationItem(Guid.NewGuid(), "alice@example.com", now.AddDays(-2))]);

        await BuildJob(facade, new FakeTimeProvider(now)).Execute(JobContext());

        (await LoadBulkEmailJobsAsync()).ShouldBeEmpty();
        await facade.DidNotReceiveWithAnyArgs().GetRegistrationsAsync(default, default, default!, default);
    }

    // Given active policies for two events
    // When the single hourly evaluation runs
    // Then each eligible event is committed independently
    [TestMethod]
    public async ValueTask Execute_MultipleActivePolicies_CreatesOneJobPerEligibleEvent()
    {
        var firstEvent = TicketedEventId.New();
        var secondEvent = TicketedEventId.New();
        var now = DateTimeOffset.UtcNow;
        await SeedPolicyAsync(firstEvent, now);
        await SeedPolicyAsync(secondEvent, now);
        var facade = Substitute.For<IRegistrationsFacade>();
        facade.GetRegistrationsAsync(
                Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<QueryRegistrationsDto>(), Arg.Any<CancellationToken>())
            .Returns(call => Task.FromResult<IReadOnlyList<RegistrationListItemDto>>([
                RegistrationItem(Guid.NewGuid(), $"{call.ArgAt<Guid>(1)}@example.com", now.AddDays(-2))]));

        await BuildJob(facade, new FakeTimeProvider(now)).Execute(JobContext());

        (await LoadBulkEmailJobsAsync()).Count.ShouldBe(2);
    }

    private async Task SeedPolicyAsync(
        TicketedEventId eventId,
        DateTimeOffset now,
        TimeOnly? quietStart = null,
        TimeOnly? quietEnd = null,
        DateTimeOffset? opensAt = null,
        DateTimeOffset? closesAt = null,
        bool archived = false,
        bool withoutPolicy = false,
        bool withoutEventContext = false,
        string timeZone = "UTC")
    {
        var builder = new EventEmailContextViewBuilder()
            .ForTeam(TeamId)
            .ForEvent(eventId)
            .At(now)
            .WithTimeZone(timeZone)
            .WithWindow(opensAt ?? now.AddHours(-1), closesAt ?? now.AddHours(1));
        if (quietStart.HasValue && quietEnd.HasValue)
            builder.WithQuietHours(quietStart.Value, quietEnd.Value);
        if (archived)
            builder.Archived();
        if (withoutPolicy)
            builder.WithoutReconfirmPolicy();
        if (withoutEventContext)
            builder.WithoutEventContext();

        await Environment.EmailDatabase.SeedAsync(db => db.EventEmailContexts.Add(builder.Build()));
    }

    private static RegistrationListItemDto RegistrationItem(
        Guid registrationId,
        string email,
        DateTimeOffset createdAt,
        int? effectiveMaxReconfirmAttempts = null) =>
        new(registrationId, email, "Alice", "Test", [], new Dictionary<string, string>(), createdAt,
            RegistrationStatus.Registered, false, null, effectiveMaxReconfirmAttempts);

    private static IRegistrationsFacade FacadeReturning(
        TicketedEventId eventId,
        IReadOnlyList<RegistrationListItemDto> candidates)
    {
        var facade = Substitute.For<IRegistrationsFacade>();
        facade.GetRegistrationsAsync(
                TeamId.Value,
                eventId.Value,
                Arg.Is<QueryRegistrationsDto>(q => MatchesReconfirmQuery(q)),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(candidates));
        return facade;
    }

    private static bool MatchesReconfirmQuery(QueryRegistrationsDto? query) =>
        query is not null
        && query.RegistrationStatus == RegistrationStatus.Registered
        && query.HasReconfirmed == false;

    private static EmailLog ReconfirmEmailLog(TicketedEventId eventId, string email, DateTimeOffset sentAt) =>
        EmailLog.Create(TeamId, eventId, $"reconfirm:{Guid.NewGuid():N}", EmailAddress.From(email),
            BuiltInEmailTemplateNames.Reconfirmation, "Please reconfirm", EmailLogStatus.Sent, sentAt, sentAt);

    private RequestReconfirmationsJob BuildJob(
        IRegistrationsFacade facade,
        TimeProvider timeProvider)
    {
        var ctx = Environment.EmailDatabase.Context;
        var services = new ServiceCollection()
            .AddScoped<IEmailWriteStore>(_ => new TestEmailWriteStore(ctx))
            .AddScoped<IRegistrationsFacade>(_ => facade)
            .AddKeyedScoped<IOutbox>(EmailModule.Key, (_, _) => new Outbox(ctx))
            .AddKeyedScoped<IUnitOfWork>(EmailModule.Key, (_, _) => new UnitOfWork<EmailDbContext>(
                ctx, new NoOpOutboxMessageSender(), NullLogger<UnitOfWork<EmailDbContext>>.Instance))
            .BuildServiceProvider();

        return new RequestReconfirmationsJob(
            ctx,
            services.GetRequiredService<IServiceScopeFactory>(),
            timeProvider,
            NullLogger<RequestReconfirmationsJob>.Instance);
    }

    private static IJobExecutionContext JobContext()
    {
        var context = Substitute.For<IJobExecutionContext>();
        context.CancellationToken.Returns(CancellationToken.None);
        return context;
    }

    private sealed class TestEmailWriteStore(EmailDbContext context) : IEmailWriteStore
    {
        public DbSet<EmailLog> EmailLog => context.EmailLog;
        public DbSet<BulkEmailJob> BulkEmailJobs => context.BulkEmailJobs;
    }

    private async Task<List<BulkEmailJob>> LoadBulkEmailJobsAsync()
    {
        Environment.EmailDatabase.Context.ChangeTracker.Clear();
        return await Environment.EmailDatabase.Context.BulkEmailJobs.AsNoTracking().ToListAsync();
    }

    private async Task<List<OutboxMessage>> LoadOutboxMessagesAsync()
    {
        Environment.EmailDatabase.Context.ChangeTracker.Clear();
        return await Environment.EmailDatabase.Context.OutboxMessages.AsNoTracking().ToListAsync();
    }

    private static IReadOnlyList<Guid> GetRegistrationIds(JsonDocument payload) =>
        payload.RootElement.GetProperty("registrationIds")
            .EnumerateArray()
            .Select(x => x.GetGuid())
            .ToList();
}
