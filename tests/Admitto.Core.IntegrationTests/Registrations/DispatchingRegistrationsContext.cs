using Amolenk.Admitto.Core.Registrations;
using Amolenk.Admitto.Core.Registrations.Application.Persistence;
using Amolenk.Admitto.Core.Registrations.Infrastructure.Persistence;
using Amolenk.Admitto.Core.Shared.Application.Auth;
using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Amolenk.Admitto.Core.Shared.Application.Persistence;
using Amolenk.Admitto.Core.Shared.Contracts;
using Amolenk.Admitto.Core.Shared.Infrastructure.Messaging;
using Amolenk.Admitto.Core.Shared.Infrastructure.Persistence.Interceptors;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Amolenk.Admitto.Core.IntegrationTests.Registrations;

/// <summary>
/// A <see cref="RegistrationsDbContext"/> whose saves go through the real <see cref="DomainEventsInterceptor"/>, with
/// the Registrations module's domain event and command handlers registered as in production. Use it for flows that
/// cascade through domain event handlers in one unit of work (e.g. a cancellation that releases a seat and offers it
/// to the waitlist), which the shared test context — deliberately without the interceptor — does not run.
/// Integration events are captured in <see cref="PublishedIntegrationEvents"/> instead of being published.
/// </summary>
internal sealed class DispatchingRegistrationsContext : IAsyncDisposable
{
    private readonly ServiceProvider _serviceProvider;

    private readonly RecordingOutbox _outbox;

    private DispatchingRegistrationsContext(
        ServiceProvider serviceProvider,
        RegistrationsDbContext context,
        RecordingOutbox outbox)
    {
        _serviceProvider = serviceProvider;
        _outbox = outbox;
        Context = context;
    }

    public RegistrationsDbContext Context { get; }

    public IUnitOfWork UnitOfWork => new DbContextUnitOfWork(Context);

    public IServiceScopeFactory ScopeFactory => _serviceProvider.GetRequiredService<IServiceScopeFactory>();

    public IReadOnlyList<IIntegrationEvent> PublishedIntegrationEvents => _outbox.IntegrationEvents;

    public static DispatchingRegistrationsContext Create(
        IntegrationTestEnvironment environment,
        TimeProvider? timeProvider = null)
    {
        var connectionString = environment.RegistrationsDatabase.Context.Database.GetConnectionString()
            ?? throw new InvalidOperationException("Registrations database connection string not available.");

        RegistrationsDbContext? context = null;
        var outbox = new RecordingOutbox();
        var assembly = typeof(RegistrationsModule).Assembly;

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(timeProvider ?? TimeProvider.System);
        services.AddKeyedSingleton<IOutbox>(RegistrationsModule.Key, outbox);
        services.AddSingleton<IRegistrationsWriteStore>(_ => context!);
        services.AddSingleton<IRegistrationsReadStore>(_ => context!);
        services.AddKeyedSingleton<IUnitOfWork>(RegistrationsModule.Key, (_, _) => new DbContextUnitOfWork(context!));
        services.AddCommandHandlersFromAssembly(assembly, RegistrationsModule.NamespacePrefix, includeWorkerHandlers: true);
        services.AddDomainEventHandlersFromAssembly(assembly, RegistrationsModule.NamespacePrefix);
        var serviceProvider = services.BuildServiceProvider();

        var options = new DbContextOptionsBuilder<RegistrationsDbContext>()
            .UseNpgsql(
                connectionString,
                npgsql => npgsql.MigrationsHistoryTable("ef_migrations_history", RegistrationsDbContext.SchemaName))
            .AddInterceptors(
                new DomainEventsInterceptor(serviceProvider),
                new AuditInterceptor(new StaticUserContextAccessor(
                    new UserContextDto(Guid.NewGuid(), "Test User", "test.user@example.com"))))
            .Options;
        context = new RegistrationsDbContext(options);

        return new DispatchingRegistrationsContext(serviceProvider, context, outbox);
    }

    /// <summary>
    /// Saves the pending changes, dispatching domain events first, then clears the change tracker.
    /// </summary>
    public async ValueTask SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await Context.SaveChangesAsync(cancellationToken);
        Context.ChangeTracker.Clear();
    }

    public async ValueTask DisposeAsync()
    {
        await Context.DisposeAsync();
        await _serviceProvider.DisposeAsync();
    }

    private sealed class RecordingOutbox : IOutbox
    {
        public List<IIntegrationEvent> IntegrationEvents { get; } = [];

        public void Enqueue(ICommand command)
        {
        }

        public void Enqueue(IIntegrationEvent integrationEvent) => IntegrationEvents.Add(integrationEvent);
    }
}
