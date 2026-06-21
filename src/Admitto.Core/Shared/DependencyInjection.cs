using Amolenk.Admitto.Core.Shared.Application.Auth;
using Amolenk.Admitto.Core.Shared.Application.Cryptography;
using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Amolenk.Admitto.Core.Shared.Application.Persistence;
using Amolenk.Admitto.Core.Shared.Infrastructure.Messaging;
using Amolenk.Admitto.Core.Shared.Infrastructure.Messaging.ServiceBus;
using Amolenk.Admitto.Core.Shared.Infrastructure.Persistence;
using Amolenk.Admitto.Core.Shared.Infrastructure.Persistence.Interceptors;
using Amolenk.Admitto.Core.Shared.Infrastructure.Persistence.Inbox;
using Amolenk.Admitto.Core.Shared.Infrastructure.Persistence.Outbox;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Azure;
using FluentValidation;
using FluentValidation.Internal;
using Humanizer;
using Quartz;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

public static class DependencyInjection
{
    public static IServiceCollection AddCryptographyApplicationServices(this IServiceCollection services)
    {
        services.AddSingleton<ISigningService, SigningService>();

        return services;
    }

    public static IServiceCollection AddValidationApplicationServices(this IServiceCollection services)
    {
        // Use camel case for FluentValidation property names
        ValidatorOptions.Global.DisplayNameResolver = (_, member, _) => member?.Name.Humanize();
        ValidatorOptions.Global.PropertyNameResolver = (_, memberInfo, expression) =>
        {
            if (expression != null)
            {
                var chain = PropertyChain.FromExpression(expression);
                if (chain.Count > 0)
                {
                    var propertyNames = chain.ToString().Split(ValidatorOptions.Global.PropertyChainSeparator);
                    if (propertyNames.Length == 1)
                    {
                        return propertyNames[0].Camelize();
                    }

                    return string.Join(
                        ValidatorOptions.Global.PropertyChainSeparator,
                        propertyNames.Select(n => n.Camelize()));
                }
            }

            return memberInfo?.Name.Camelize();
        };

        return services;
    }

    public static IServiceCollection AddSharedInfrastructureServices(this IServiceCollection services)
    {
        services.AddScoped<IOutboxMessageSender, OutboxMessageSender>();

        return services;
    }

    public static IHostApplicationBuilder AddSharedInfrastructureMessagingServices(
        this IHostApplicationBuilder builder)
    {
        // Configure a short TryTimeout so the SDK re-issues AMQP receive requests frequently.
        // The Azure SB emulator only checks for new messages when it receives a fresh AMQP credit;
        // with the default 60 s TryTimeout a message arriving after the initial check sits undelivered
        // for up to 90 s. 5 s keeps emulator tests fast and has no adverse effect on production SB.
        builder.AddAzureServiceBusClient(
            connectionName: "messaging",
            configureClientBuilder: clientBuilder => clientBuilder.ConfigureOptions(options =>
            {
                options.RetryOptions.TryTimeout = TimeSpan.FromSeconds(5);
            }));

        builder.Services.AddSingleton<ServiceBusSender>(serviceProvider =>
        {
            var client = serviceProvider.GetRequiredService<ServiceBusClient>();
            return client.CreateSender("queue");
        });

        return builder;
    }

    /// <summary>
    /// Registers the queue consumer pipeline (dispatcher and the <see cref="BackgroundService"/> that
    /// polls the queue), and starts the Quartz job scheduler hosted service.
    /// Call <see cref="AddMessageTypeRegistry"/> separately to register the message type registry.
    /// Each module registers its own Quartz jobs via <c>AddQuartz</c> in its worker setup method.
    /// </summary>
    public static IHostApplicationBuilder AddSharedInfrastructureQueueConsumer(
        this IHostApplicationBuilder builder)
    {
        builder.Services.AddScoped<QueueMessageDispatcher>();
        builder.Services.AddHostedService<ServiceBusMessageProcessor>();

        builder.Services.AddQuartzHostedService(options =>
        {
            options.WaitForJobsToComplete = true;
        });

        return builder;
    }

    public static IHostApplicationBuilder AddSharedQuartzInfrastructure(this IHostApplicationBuilder builder)
    {
        var quartzConnectionString = builder.Configuration.GetConnectionString("quartz-db");
        if (string.IsNullOrWhiteSpace(quartzConnectionString))
            return builder;

        builder.Services.AddQuartz(options =>
        {
            options.SchedulerName = "Admitto";
            options.SchedulerId = "AUTO";

            options.UsePersistentStore(store =>
            {
                store.UseProperties = true;
                store.UsePostgres(quartzConnectionString);
                store.UseSystemTextJsonSerializer();
                store.UseClustering(cluster =>
                {
                    cluster.CheckinInterval = TimeSpan.FromSeconds(10);
                    cluster.CheckinMisfireThreshold = TimeSpan.FromSeconds(30);
                });
            });
        });

        return builder;
    }

    // /// <summary>
    // /// Registers the message type registry singleton, built lazily from all
    // /// <see cref="Action{MessageTypeRegistryBuilder}"/> contributions registered by each module's
    // /// <c>AddXModule</c> call.
    // /// </summary>
    // public static IHostApplicationBuilder AddMessageTypeRegistry(this IHostApplicationBuilder builder)
    // {
    //     builder.Services.AddSingleton(sp =>
    //     {
    //         var registryBuilder = new MessageTypeRegistryBuilder();
    //         foreach (var configure in sp.GetServices<Action<MessageTypeRegistryBuilder>>())
    //             configure(registryBuilder);
    //         return registryBuilder.Build();
    //     });
    //     return builder;
    // }

    /// <summary>
    /// Convenience wrapper that registers all shared cross-cutting services needed by both the API
    /// and Worker hosts: messaging infrastructure, outbox sender, cryptography, validation config,
    /// and the message type registry built from module contributions.
    /// Worker hosts should additionally call <see cref="AddSharedInfrastructureQueueConsumer"/>.
    /// </summary>
    public static IHostApplicationBuilder AddSharedServices(this IHostApplicationBuilder builder)
    {
        builder.AddSharedInfrastructureMessagingServices();
        builder.AddSharedQuartzInfrastructure();
        builder.Services
            .AddSharedInfrastructureServices()
            .AddCryptographyApplicationServices()
            .AddValidationApplicationServices();
        // builder.AddMessageTypeRegistry();
        return builder;
    }

    public static IHostApplicationBuilder AddModuleDatabaseServices<TWriteModel, TDbContext>(
        this IHostApplicationBuilder builder,
        string moduleKey)
        where TDbContext : DbContext, IModuleDbContext, TWriteModel
        where TWriteModel : class
    {
        var admittoConnectionString = builder.Configuration.GetConnectionString("admitto-db")!;

        builder.Services.AddDbContext<TDbContext>((sp, options) =>
        {
            options.UseNpgsql(
                admittoConnectionString,
                ModuleNpgsqlOptions.ConfigureMigrationsHistory<TDbContext>);

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
                    var logger = sp.GetRequiredService<ILogger<UnitOfWork<TDbContext>>>();
                    var postgresExceptionMapping = sp.GetKeyedService<IPostgresExceptionMapping>(key);
                    return new UnitOfWork<TDbContext>(
                        dbContext,
                        outboxMessageSender,
                        logger,
                        postgresExceptionMapping);
                });

        if (typeof(IOutboxDbContext).IsAssignableFrom(typeof(TDbContext)))
        {
            builder.Services.AddKeyedScoped<IOutbox>(moduleKey, (sp, _) =>
                new Outbox((IOutboxDbContext)sp.GetRequiredService<TDbContext>()));
        }

        if (typeof(IInboxDbContext).IsAssignableFrom(typeof(TDbContext)))
        {
            builder.Services.AddKeyedScoped<IInbox>(moduleKey, (sp, _) =>
                new Inbox((IInboxDbContext)sp.GetRequiredService<TDbContext>()));
        }

        return builder;
    }
}
