using Amolenk.Admitto.Core.Email;
using Amolenk.Admitto.Core.Email.Application.UseCases.Emails.DeliverEmail;
using Amolenk.Admitto.Core.Email.Infrastructure.Persistence;
using Amolenk.Admitto.Core.Shared.Infrastructure.Persistence.Outbox;
using Amolenk.Admitto.Testing.Infrastructure.Assertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Amolenk.Admitto.Core.IntegrationTests.Shared.Infrastructure.Persistence.Outbox;

[TestClass]
public sealed class OutboxDispatcherTests(TestContext testContext) : AspireIntegrationTestBase
{
    // Given a pending outbox message old enough to exceed the minimum age
    // When orphaned messages are dispatched
    // Then the message is sent and marked as sent
    [TestMethod]
    public async ValueTask DispatchOrphanedAsync_PendingRow_SendsAndMarksSent()
    {
        var message = NewDeliveryMessage("outbox-test", DateTimeOffset.UtcNow.AddMinutes(-1));

        await Environment.EmailDatabase.SeedAsync(db => db.OutboxMessages.Add(message));

        Environment.EmailDatabase.Context.ChangeTracker.Clear();
        var sender = new RecordingOutboxMessageSender();
        var dispatcher = new OutboxDispatcher(Environment.EmailDatabase.Context, sender);

        var dispatched = await dispatcher.DispatchOrphanedAsync(
            batchSize: 10,
            minimumAge: TimeSpan.FromSeconds(5),
            cancellationToken: testContext.CancellationToken);

        dispatched.ShouldBeTrue();
        sender.SentMessageIds.ShouldBe([message.Id]);

        var reloaded = await Environment.EmailDatabase.Context.OutboxMessages
            .AsNoTracking()
            .SingleAsync(m => m.Id == message.Id, testContext.CancellationToken);
        reloaded.State.ShouldBe(OutboxMessageState.Sent);
    }

    // Given a pending outbox message that is too recent to exceed the minimum age
    // When orphaned messages are dispatched
    // Then the message is not sent and remains pending
    [TestMethod]
    public async ValueTask DispatchOrphanedAsync_RecentPendingRow_DoesNotSend()
    {
        var message = NewDeliveryMessage("outbox-recent-test", DateTimeOffset.UtcNow);
        await Environment.EmailDatabase.SeedAsync(db => db.OutboxMessages.Add(message));

        Environment.EmailDatabase.Context.ChangeTracker.Clear();
        var sender = new RecordingOutboxMessageSender();
        var dispatcher = new OutboxDispatcher(Environment.EmailDatabase.Context, sender);

        var dispatched = await dispatcher.DispatchOrphanedAsync(
            batchSize: 10,
            minimumAge: TimeSpan.FromSeconds(5),
            cancellationToken: testContext.CancellationToken);

        dispatched.ShouldBeFalse();
        sender.SentMessageIds.ShouldBeEmpty();

        var reloaded = await Environment.EmailDatabase.Context.OutboxMessages
            .AsNoTracking()
            .SingleAsync(m => m.Id == message.Id, testContext.CancellationToken);
        reloaded.State.ShouldBe(OutboxMessageState.Pending);
    }

    // Given a pending outbox message old enough to exceed the minimum age
    // When the outbox retry background service runs
    // Then the message is eventually sent and marked as sent
    [TestMethod]
    public async ValueTask OutboxRetryBackgroundService_PendingRow_SendsAndMarksSent()
    {
        var message = NewDeliveryMessage("outbox-background-test", DateTimeOffset.UtcNow.AddMinutes(-1));
        await Environment.EmailDatabase.SeedAsync(db => db.OutboxMessages.Add(message));

        var sender = new RecordingOutboxMessageSender();
        var services = new ServiceCollection();
        services.AddSingleton(Environment.EmailDatabase.Context);
        services.AddSingleton<IOutboxMessageSender>(sender);
        services.AddSingleton(new OutboxDbContextRegistration(EmailModule.Key, typeof(EmailDbContext)));

        var provider = services.BuildServiceProvider();
        var service = new OutboxRetryBackgroundService(
            provider.GetRequiredService<IServiceScopeFactory>(),
            new StaticOptionsMonitor<OutboxRetryOptions>(new OutboxRetryOptions
            {
                BatchSize = 10,
                PollingInterval = TimeSpan.FromMinutes(5),
                MinimumAge = TimeSpan.FromSeconds(5)
            }),
            NullLogger<OutboxRetryBackgroundService>.Instance);

        await service.StartAsync(testContext.CancellationToken);
        try
        {
            await ShouldEventually.Succeed(async () =>
            {
                sender.SentMessageIds.ShouldContain(message.Id);

                var reloaded = await Environment.EmailDatabase.Context.OutboxMessages
                    .AsNoTracking()
                    .SingleAsync(m => m.Id == message.Id, testContext.CancellationToken);
                reloaded.State.ShouldBe(OutboxMessageState.Sent);
            }, TimeSpan.FromSeconds(10), TimeSpan.FromMilliseconds(100));
        }
        finally
        {
            await service.StopAsync(CancellationToken.None);
        }
    }

    // Given a pending outbox message being dispatched concurrently by two scanner instances
    // When both dispatchers race to send the same message
    // Then both sends occur but the row ends up marked as sent
    [TestMethod]
    public async ValueTask DispatchOrphanedAsync_DuplicateScannerRace_LeavesRowSent()
    {
        var message = NewDeliveryMessage("outbox-race-test", DateTimeOffset.UtcNow.AddMinutes(-1));
        await Environment.EmailDatabase.SeedAsync(db => db.OutboxMessages.Add(message));

        await using var firstContext = CreateEmailDbContext();
        await using var secondContext = CreateEmailDbContext();
        var sender = new BlockingOutboxMessageSender(expectedSenders: 2);
        var firstDispatcher = new OutboxDispatcher(firstContext, sender);
        var secondDispatcher = new OutboxDispatcher(secondContext, sender);

        var first = firstDispatcher.DispatchOrphanedAsync(
            batchSize: 10,
            minimumAge: TimeSpan.FromSeconds(5),
            cancellationToken: testContext.CancellationToken).AsTask();
        var second = secondDispatcher.DispatchOrphanedAsync(
            batchSize: 10,
            minimumAge: TimeSpan.FromSeconds(5),
            cancellationToken: testContext.CancellationToken).AsTask();

        await sender.WaitForAllSendersAsync(testContext.CancellationToken);
        sender.ReleaseSenders();

        await Task.WhenAll(first, second);

        sender.SentMessageIds.Count(id => id == message.Id).ShouldBe(2);

        var reloaded = await Environment.EmailDatabase.Context.OutboxMessages
            .AsNoTracking()
            .SingleAsync(m => m.Id == message.Id, testContext.CancellationToken);
        reloaded.State.ShouldBe(OutboxMessageState.Sent);
    }

    private static OutboxMessage NewDeliveryMessage(string idempotencyKey, DateTimeOffset createdAt) =>
        OutboxMessage.From(new DeliverEmailCommand(
            TeamId: Guid.NewGuid(),
            TicketedEventId: Guid.NewGuid(),
            RecipientAddress: "alice@example.com",
            RecipientName: "Alice",
            EmailType: "ticket",
            IdempotencyKey: idempotencyKey,
            Subject: "Subject",
            TextBody: "Text",
            HtmlBody: "<p>Html</p>"),
            createdAt);

    private static EmailDbContext CreateEmailDbContext()
    {
        var connectionString = Environment.EmailDatabase.Context.Database.GetConnectionString()
            ?? throw new InvalidOperationException("Email database connection string not available.");

        var options = new DbContextOptionsBuilder<EmailDbContext>()
            .UseNpgsql(
                connectionString,
                npgsql => npgsql.MigrationsHistoryTable("ef_migrations_history", EmailDbContext.SchemaName))
            .Options;

        return new EmailDbContext(options);
    }

    private sealed class RecordingOutboxMessageSender : IOutboxMessageSender
    {
        public List<Guid> SentMessageIds { get; } = [];

        public ValueTask SendAsync(OutboxMessage message, CancellationToken cancellationToken = default)
        {
            SentMessageIds.Add(message.Id);
            return ValueTask.CompletedTask;
        }
    }

    private sealed class BlockingOutboxMessageSender(int expectedSenders) : IOutboxMessageSender
    {
        private readonly TaskCompletionSource _allSendersArrived = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource _releaseSenders = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly object _gate = new();
        private int _senderCount;

        public List<Guid> SentMessageIds { get; } = [];

        public async ValueTask SendAsync(OutboxMessage message, CancellationToken cancellationToken = default)
        {
            lock (_gate)
            {
                SentMessageIds.Add(message.Id);
            }

            if (Interlocked.Increment(ref _senderCount) == expectedSenders)
                _allSendersArrived.SetResult();

            await _releaseSenders.Task.WaitAsync(cancellationToken);
        }

        public async Task WaitForAllSendersAsync(CancellationToken cancellationToken) =>
            await _allSendersArrived.Task.WaitAsync(cancellationToken);

        public void ReleaseSenders() => _releaseSenders.SetResult();
    }

    private sealed class StaticOptionsMonitor<T>(T value) : IOptionsMonitor<T>
    {
        public T CurrentValue => value;
        public T Get(string? name) => value;
        public IDisposable? OnChange(Action<T, string?> listener) => null;
    }
}
