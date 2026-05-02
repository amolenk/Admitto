using Amolenk.Admitto.Module.Shared.Application.Auth;
using Amolenk.Admitto.Module.Shared.Application.Messaging;
using Amolenk.Admitto.Module.Shared.Application.Persistence;
using Amolenk.Admitto.Module.Shared.Infrastructure.Messaging;
using Amolenk.Admitto.Module.Shared.Infrastructure.Persistence;
using Amolenk.Admitto.Module.Shared.Infrastructure.Persistence.Interceptors;
using Amolenk.Admitto.Module.Shared.Infrastructure.Persistence.Outbox;
using Azure.Storage.Queues;
using Microsoft.EntityFrameworkCore;

namespace Amolenk.Admitto.Module.Shared.Infrastructure;

public static class DependencyInjection
{
    extension(IServiceCollection services)
    {
        public void AddSharedInfrastructureServices()
        {
            services.AddScoped<IOutboxMessageSender, OutboxMessageSender>();
        }
    }

    extension(IHostApplicationBuilder builder)
    {
        public IHostApplicationBuilder AddSharedInfrastructureMessagingServices()
        {
            builder.AddAzureQueueServiceClient(connectionName: "queues");
            
            builder.Services.AddSingleton<QueueClient>(serviceProvider =>
            {
                var queueServiceClient = serviceProvider.GetRequiredService<QueueServiceClient>();
                return queueServiceClient.GetQueueClient("queue");
            });

            return builder;
        }

        /// <summary>
        /// Registers the queue consumer pipeline (message-type registry, routers, dispatcher
        /// and the <see cref="BackgroundService"/> that polls the queue). Only hosts that
        /// own queue consumption (the Worker) should call this.
        /// </summary>
        public IHostApplicationBuilder AddSharedInfrastructureQueueConsumer()
        {
            // Snapshot of currently-loaded assemblies; module assemblies are loaded
            // by the time DI configuration runs so all event types are discoverable.
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();

            builder.Services.AddSingleton(new MessageTypeRegistry(assemblies));
            builder.Services.AddScoped<IntegrationEventRouter>();
            builder.Services.AddScoped<ModuleEventRouter>();
            builder.Services.AddScoped<QueueMessageDispatcher>();

            builder.Services.AddHostedService<MessageQueueProcessor>();

            return builder;
        }
        
        public IHostApplicationBuilder AddModuleDatabaseServices<TWriteModel, TDbContext>(string moduleKey)
            where TDbContext : DbContext, IModuleDbContext, TWriteModel
            where TWriteModel : class
        {
            var admittoConnectionString = builder.Configuration.GetConnectionString("admitto-db")!;

            builder.Services.AddDbContext<TDbContext>((sp, options) =>
            {
                options.UseNpgsql(
                    admittoConnectionString,
                    npgsql =>
                    {
                        npgsql.MigrationsHistoryTable("ef_migrations_history", TDbContext.SchemaName);
                    });
            
                options.AddInterceptors(
                    new AuditInterceptor(sp.GetRequiredService<IUserContextAccessor>()),
                    new DomainEventsInterceptor(sp, moduleKey));
            });

            builder.EnrichNpgsqlDbContext<TDbContext>();

            builder.Services
                .AddScoped<TWriteModel>(sp => sp.GetRequiredService<TDbContext>())
                .AddKeyedScoped<IUnitOfWork, UnitOfWork<TDbContext>>(
                    moduleKey,
                    (sp, key) =>
                    {
                        var dbContext = sp.GetRequiredService<TDbContext>();
                        var outboxMessageSender = sp.GetRequiredService<IOutboxMessageSender>();
                        var postgresExceptionMapping = sp.GetKeyedService<IPostgresExceptionMapping>(key);
                        return new UnitOfWork<TDbContext>(
                            dbContext,
                            outboxMessageSender,
                            postgresExceptionMapping);
                    });

            if (typeof(IOutboxDbContext).IsAssignableFrom(typeof(TDbContext)))
            {
                builder.Services.AddKeyedScoped<IIntegrationEventOutbox>(moduleKey, (sp, _) =>
                    new IntegrationEventOutbox((IOutboxDbContext)sp.GetRequiredService<TDbContext>()));
            }

            return builder;
        }
    }
}