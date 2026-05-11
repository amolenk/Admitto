using Amolenk.Admitto.Core.Shared.Application.Auth;
using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Amolenk.Admitto.Core.Shared.Application.Persistence;
using Amolenk.Admitto.Core.Shared.Infrastructure.Messaging;
using Amolenk.Admitto.Core.Shared.Infrastructure.Persistence;
using Amolenk.Admitto.Core.Shared.Infrastructure.Persistence.Interceptors;
using Amolenk.Admitto.Core.Shared.Infrastructure.Persistence.Outbox;
using Azure.Storage.Queues;
using Microsoft.EntityFrameworkCore;

namespace Amolenk.Admitto.Core.Shared.Infrastructure;

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
        /// Registers the queue consumer pipeline (dispatcher and the
        /// <see cref="BackgroundService"/> that polls the queue). Call
        /// <see cref="AddMessageTypeRegistry"/> separately to register the message type registry.
        /// </summary>
        public IHostApplicationBuilder AddSharedInfrastructureQueueConsumer()
        {
            builder.Services.AddScoped<QueueMessageDispatcher>();

            builder.Services.AddHostedService<MessageQueueProcessor>();

            return builder;
        }

        public IHostApplicationBuilder AddMessageTypeRegistry(Action<MessageTypeRegistryBuilder> configure)
        {
            var registryBuilder = new MessageTypeRegistryBuilder();
            configure(registryBuilder);
            builder.Services.AddSingleton(registryBuilder.Build());
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
                    new DomainEventsInterceptor(sp));
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
                builder.Services.AddKeyedScoped<IOutbox>(moduleKey, (sp, _) =>
                    new Outbox((IOutboxDbContext)sp.GetRequiredService<TDbContext>()));
            }

            return builder;
        }
    }
}