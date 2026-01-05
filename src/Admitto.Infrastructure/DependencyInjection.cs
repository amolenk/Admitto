using Amolenk.Admitto.Application.Common.Authentication;
using Amolenk.Admitto.Application.Common.Email.Sending;
using Amolenk.Admitto.Application.Common.Messaging;
using Amolenk.Admitto.Application.Common.Persistence;
using Amolenk.Admitto.Application.Common.Slugs;
using Amolenk.Admitto.Infrastructure;
using Amolenk.Admitto.Infrastructure.Email;
using Amolenk.Admitto.Infrastructure.Messaging;
using Amolenk.Admitto.Infrastructure.Migrators;
using Amolenk.Admitto.Infrastructure.Persistence;
using Amolenk.Admitto.Infrastructure.Persistence.Interceptors;
using Amolenk.Admitto.Infrastructure.UserManagement.Keycloak;
using Amolenk.Admitto.Infrastructure.UserManagement.MicrosoftGraph;
using Azure.Identity;
using Azure.Storage.Queues;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.Graph;
using Quartz;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

public static class DependencyInjection
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddInfrastructureEmailServices()
        {
            services.AddTransient<IEmailSenderFactory, SmtpEmailSenderFactory>();

            return services;
        }
    }

    extension<TBuilder>(TBuilder builder) where TBuilder : IHostApplicationBuilder
    {
        public TBuilder AddInfrastructureDatabaseServices()
        {
            var admittoConnectionString = builder.Configuration.GetConnectionString("admitto-db")!;

            builder.Services.AddDbContext<ApplicationContext>((sp, options) =>
            {
                options.UseNpgsql(admittoConnectionString);
                options.AddInterceptors(
                    new AuditInterceptor(),
                    new DomainEventsInterceptor(sp));
            });

            builder.EnrichNpgsqlDbContext<ApplicationContext>();
        
            builder.Services
                .AddScoped<IApplicationContext>(sp => sp.GetRequiredService<ApplicationContext>())
                .AddScoped<IUnitOfWork, UnitOfWork>()
                .AddScoped<ISlugResolver, SlugResolver>()
                .AddTransient<DatabaseMigrator>();

            return builder;
        }

        public TBuilder AddInfrastructureMessagingServices()
        {
            builder.AddAzureQueueServiceClient(connectionName: "queues");
            
            builder.Services.AddSingleton<QueueClient>(serviceProvider =>
            {
                var queueServiceClient = serviceProvider.GetRequiredService<QueueServiceClient>();
                return queueServiceClient.GetQueueClient(
                    Amolenk.Admitto.Infrastructure.Constants.AzureQueueStorage.DefaultQueueName);
            });
            
            builder.Services
                .AddScoped<IMessageSender, MessageSender>()
                .AddScoped<MessageOutbox>()
                .AddScoped<IMessageOutbox>(sp => sp.GetRequiredService<MessageOutbox>());

            return builder;
        }
        
        public TBuilder AddInfrastructureJobServices()
        {
            var quartzConnectionString = builder.Configuration.GetConnectionString("quartz-db")!;
            
            // Add Npgsql data source for Quartz job database.
            builder.Services.AddNpgsqlDataSource(quartzConnectionString, serviceKey: "quartz");
            
            builder.Services.AddQuartz(options =>
            {
                options.UsePersistentStore(persistenceOptions =>
                {
                    persistenceOptions.UsePostgres(cfg =>
                    {
                        cfg.ConnectionString = quartzConnectionString;
                    });

                    persistenceOptions.UseSystemTextJsonSerializer();
                    persistenceOptions.UseProperties = true;
                });
            });

            return builder;
        }
        
        public TBuilder AddInfrastructureUserManagementServices()
        {
            if (builder.Configuration.GetSection(MicrosoftGraphOptions.SectionName).Exists())
            {
                builder.AddMicrosoftGraphServices();
            }
            else if (builder.Configuration.GetSection(KeycloakOptions.SectionName).Exists())
            {
                builder.AddKeycloakServices();
            }
            else
            {
                throw new InvalidOperationException(
                    "No user management service configured. Please configure either Microsoft Graph or Keycloak settings.");
            }

            return builder;
        }
        
        private void AddMicrosoftGraphServices()
        {
            var services = builder.Services;

            services.Configure<MicrosoftGraphOptions>(
                builder.Configuration.GetSection(MicrosoftGraphOptions.SectionName));
            services.AddOptions<MicrosoftGraphOptions>()
                .ValidateDataAnnotations()
                .ValidateOnStart();

            // Register Graph Service Client with client credentials authentication
            services.AddScoped<GraphServiceClient>(serviceProvider =>
            {
                var options = serviceProvider.GetRequiredService<IOptions<MicrosoftGraphOptions>>().Value;

                var credential = new ClientSecretCredential(
                    options.TenantId,
                    options.ClientId,
                    options.ClientSecret);

                return new GraphServiceClient(credential);
            });

            services.AddScoped<IUserManagementService, MicrosoftGraphUserManagementService>();
        }

        private void AddKeycloakServices()
        {
            var services = builder.Services;

            services.Configure<KeycloakOptions>(builder.Configuration.GetSection(KeycloakOptions.SectionName));
            services.AddOptions<KeycloakOptions>()
                .ValidateDataAnnotations()
                .ValidateOnStart();

            // Register the auth handler
            services.AddSingleton<KeycloakAccessTokenHandler>();

            // Use Keycloak as the identity service
            var settings = builder.Configuration.GetSection(KeycloakOptions.SectionName).Get<KeycloakOptions>();
            services.AddHttpClient<IUserManagementService, KeycloakUserManagementService>(client =>
                {
                    // Use .NET Service Discovery to get Keycloak endpoint
                    client.BaseAddress = new Uri(settings!.Authority);
                })
                .AddHttpMessageHandler<KeycloakAccessTokenHandler>();
        }
    }
}