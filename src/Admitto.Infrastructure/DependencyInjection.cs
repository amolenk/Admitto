using System.Security.Claims;
using Amolenk.Admitto.Application.Common.Abstractions;
using Amolenk.Admitto.Application.Common.Email;
using Amolenk.Admitto.Application.Common.Email.Sending;
using Amolenk.Admitto.Infrastructure;
using Amolenk.Admitto.Infrastructure.Auth;
using Amolenk.Admitto.Infrastructure.Auth.AdminOverride;
using Amolenk.Admitto.Infrastructure.Auth.OpenFga;
using Amolenk.Admitto.Infrastructure.Email;
using Amolenk.Admitto.Infrastructure.Messaging;
using Amolenk.Admitto.Infrastructure.Migrators;
using Amolenk.Admitto.Infrastructure.Persistence;
using Amolenk.Admitto.Infrastructure.Persistence.Interceptors;
using Amolenk.Admitto.Infrastructure.UserManagement.Keycloak;
using Amolenk.Admitto.Infrastructure.UserManagement.MicrosoftGraph;
using Azure.Identity;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.Graph;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

// TODO Make a decision on keeping this here or moving it to ServiceDefaults

public static class DependencyInjection
{
    public static IHostApplicationBuilder AddDefaultInfrastructureServices(this IHostApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        var connectionString = builder.Configuration.GetConnectionString("admitto-db")!;

        builder.Services.AddDbContext<ApplicationContext>((sp, options) =>
        {
            options.UseNpgsql(connectionString);
            options.AddInterceptors(
                new AuditInterceptor(sp.GetRequiredService<IHttpContextAccessor>()),
                new DomainEventsInterceptor(sp));
        });
        
        builder.EnrichNpgsqlDbContext<ApplicationContext>();

        builder.Services.AddMemoryCache(options =>
        {
            // TODO Optimize cache size and expiration
        });
        
        builder.Services.AddTransient<DatabaseMigrator>();
        
        builder.Services.AddScoped<ISlugResolver, SlugResolver>();
        
        builder.AddAzureServiceBusClient(connectionName: "messaging");
        
        builder.Services
            .AddScoped<IUnitOfWork, UnitOfWork>();

        builder.Services
            .AddScoped<IApplicationContext>(sp => sp.GetRequiredService<ApplicationContext>());
        
        builder.Services
            .AddScoped<IMessageSender, MessageSender>()
            .AddScoped<MessageOutbox>()
            .AddScoped<IMessageOutbox>(sp => sp.GetRequiredService<MessageOutbox>());

        builder.Services.AddTransient<IEmailSenderFactory, SmtpEmailSenderFactory>();

        builder.AddAuthServices();
        
        builder.Services.AddTransient<Amolenk.Admitto.Infrastructure.Migrators.OpenFgaMigrator>();
        builder.Services.AddTransient<IMigrationService, MigrationService>();
        
        return builder;
    }
    
    // TODO Split into user management and authorization? Or keep together?
    private static void AddAuthServices(this IHostApplicationBuilder builder)
    {
        if (builder.Configuration.GetSection(MicrosoftGraphOptions.SectionName).Exists())
        {
            builder.AddMicrosoftGraphServices();
        }
        else if (builder.Configuration.GetSection(KeycloakOptions.SectionName).Exists())
        {
            builder.AddKeycloakServices();
        }

        builder.AddOpenFgaServices();
    }

    private static void AddMicrosoftGraphServices(this IHostApplicationBuilder builder)
    {
        var services = builder.Services;
        
        services.Configure<MicrosoftGraphOptions>(builder.Configuration.GetSection(MicrosoftGraphOptions.SectionName));
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

        services.AddScoped<IIdentityService, MicrosoftGraphIdentityService>();
    }
    
    private static void AddKeycloakServices(this IHostApplicationBuilder builder)
    {
        var services = builder.Services;
        
        services.Configure<KeycloakOptions>(builder.Configuration.GetSection(KeycloakOptions.SectionName));
        services.AddOptions<KeycloakOptions>()
            .ValidateDataAnnotations()
            .ValidateOnStart();
        
        // Register the auth handler
        services.AddSingleton<KeycloakAccessTokenHandler>();

        // Use Keycloak as the identity service
        services.AddHttpClient<IIdentityService, KeycloakIdentityService>(client =>
            {
                // Use .NET Service Discovery to get Keycloak endpoint
                client.BaseAddress = new Uri("https+http://keycloak");
            })
            .AddHttpMessageHandler<KeycloakAccessTokenHandler>();
    }

    private static void AddOpenFgaServices(this IHostApplicationBuilder builder)
    {
        var services = builder.Services;

        services.AddHttpClient<OpenFgaClientFactory>(client =>
        {
            var openFgaEndpoint = builder.Configuration["services:openfga:http:0"];
            
            // The OpenFGA SDK doesn't play nice with .NET Service Discovery, because we need to explicitly set the
            // API URL in the ClientConfiguration, even when providing a custom HttpClient.
            // We can still get the Service Discovery URL from the configuration ourselves and set it on the HttpClient.
            client.BaseAddress = new Uri(openFgaEndpoint!);

            // TODO For production, read store and model ids from configuration
        });
        
        services.AddTransient<OpenFgaMigrator>();
        
        services.AddKeyedScoped<IAuthorizationService, OpenFgaAuthorizationService>("OpenFga");
        
        // TODO Not actually part of OpenFGA, but here is as good a place as any
        services.AddScoped<IAuthorizationService>(sp => new AdminOverrideAuthorizationService(
            sp.GetRequiredKeyedService<IAuthorizationService>("OpenFga"),
            sp.GetRequiredService<IConfiguration>()));
    }
}
