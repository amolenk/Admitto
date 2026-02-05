using System.Text.Json;
using Amolenk.Admitto.Shared.Application.Messaging;
using Amolenk.Admitto.Shared.Application.Persistence;
using Amolenk.Admitto.Shared.Infrastructure.Persistence;
using Amolenk.Admitto.Shared.Infrastructure.Persistence.Interceptors;
using Amolenk.Admitto.Shared.Infrastructure.Persistence.Outbox;
using Amolenk.Admitto.Shared.Infrastructure.Serialization;
using Amolenk.Admitto.Shared.Kernel.ValueObjects;
using Azure.Storage.Queues;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.EntityFrameworkCore;

namespace Amolenk.Admitto.Shared.Infrastructure;

public static class DependencyInjection
{
    extension(IServiceCollection services)
    {
        public void AddSharedInfrastructureJsonConverters()
        {
            services.Configure<JsonOptions>(options =>
            {
                var converters = options.SerializerOptions.Converters;
                converters.Add(new GuidValueObjectJsonConverter<TicketedEventId>());
            });
        }
        
        public void AddSharedInfrastructureServices()
        {
            services.AddScoped<IOutboxMessageSender, OutboxMessageSender>();
            
            services.AddSharedInfrastructureJsonConverters();
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
            
            // builder.Services
            //     .AddScoped<IMessageSender, MessageSender>()
            //     .AddScoped<MessageOutbox>()
            //     .AddScoped<IMessageOutbox>(sp => sp.GetRequiredService<MessageOutbox>());

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
                    new AuditInterceptor(),
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
                        var mediator = sp.GetRequiredService<IMediator>();
                        var outboxMessageSender = sp.GetRequiredService<IOutboxMessageSender>();
                        var postgresExceptionMapper = sp.GetKeyedService<IPostgresExceptionMapper>(moduleKey);
                        return new UnitOfWork<TDbContext>(
                            dbContext,
                            mediator,
                            outboxMessageSender,
                            postgresExceptionMapper);
                    });

            return builder;
        }

    }
}