using Amolenk.Admitto.Application.Common.Abstractions;
using Amolenk.Admitto.Application.UseCases.Email;
using Amolenk.Admitto.Infrastructure;
using Amolenk.Admitto.Infrastructure.Auth;
using Amolenk.Admitto.Infrastructure.Email;
using Amolenk.Admitto.Infrastructure.Messaging;
using Amolenk.Admitto.Infrastructure.Persistence;
using Azure.Identity;
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
            options.AddInterceptors(new DomainEventsInterceptor(sp));
        });
        
        builder.EnrichNpgsqlDbContext<ApplicationContext>();
        
        builder.AddKeyedAzureQueueClient("queues");
        
        builder.Services
            .AddScoped<IUnitOfWork, UnitOfWork>();

        builder.Services
            .AddScoped<IDomainContext>(sp => sp.GetRequiredService<ApplicationContext>())
            .AddScoped<IReadModelContext>(sp => sp.GetRequiredService<ApplicationContext>())
            .AddScoped<IEmailContext>(sp => sp.GetRequiredService<ApplicationContext>());
        
        builder.Services
            .AddScoped<MessageOutbox>()
            .AddScoped<IMessageOutbox>(sp => sp.GetRequiredService<MessageOutbox>());

        builder.Services.AddScoped<IEmailOutbox, EmailOutbox>();
        
        builder.AddAuthServices();
        
        return builder;
    }
    
    public static IHostApplicationBuilder AddSmtpEmailServices(this IHostApplicationBuilder builder)
    {
        builder.Services.AddTransient<IEmailSender, SmtpEmailSender>();

        return builder;
    }

    private static void AddAuthServices(this IHostApplicationBuilder builder)
    {
        // Configure identity provider selection
        builder.Services.Configure<IdentityProviderOptions>(builder.Configuration.GetSection("IdentityProvider"));
        
        var identityProvider = builder.Configuration.GetSection("IdentityProvider")["Provider"] ?? IdentityProviders.Keycloak;
        
        switch (identityProvider)
        {
            case IdentityProviders.Keycloak:
                builder.AddKeycloakServices();
                break;
            case IdentityProviders.EntraId:
                builder.AddEntraIdServices();
                break;
            default:
                throw new InvalidOperationException($"Unsupported identity provider: {identityProvider}");
        }
        
        builder.AddOpenFgaServices();
    }

    private static void AddEntraIdServices(this IHostApplicationBuilder builder)
    {
        var services = builder.Services;
        
        services.Configure<EntraIdOptions>(builder.Configuration.GetSection("EntraId"));
        services.AddOptions<EntraIdOptions>()
            .ValidateDataAnnotations()
            .ValidateOnStart();

        // Register Graph Service Client with client credentials authentication
        services.AddScoped<GraphServiceClient>(serviceProvider =>
        {
            var options = serviceProvider.GetRequiredService<IOptions<EntraIdOptions>>().Value;
            
            var credential = new ClientSecretCredential(
                options.TenantId,
                options.ClientId,
                options.ClientSecret);

            return new GraphServiceClient(credential);
        });

        // Register EntraId as the identity service
        services.AddScoped<IIdentityService, EntraIdIdentityService>();
    }

    private static void AddKeycloakServices(this IHostApplicationBuilder builder)
    {
        var services = builder.Services;
        
        services.Configure<AccessTokenOptions>(builder.Configuration.GetSection("KeycloakApi"));
        services.AddOptions<AccessTokenOptions>()
            .ValidateDataAnnotations()
            .ValidateOnStart();
        
        // Register the auth handler
        services.AddSingleton<AccessTokenHandler>();

        // Use Keycloak as the identity service
        services.AddHttpClient<IIdentityService, KeycloakIdentityService>(client =>
            {
                // Use .NET Service Discovery to get Keycloak endpoint
                client.BaseAddress = new Uri("https+http://keycloak");
            })
            .AddHttpMessageHandler<AccessTokenHandler>();
    }

    private static void AddOpenFgaServices(this IHostApplicationBuilder builder)
    {
        var services = builder.Services;

        services.AddHttpClient<OpenFgaClientFactory>(client =>
        {
            // The OpenFGA SDK doesn't play nice with .NET Service Discovery, because we need to explicitly set the
            // API URL in the ClientConfiguration, even when providing a custom HttpClient.
            // We can still get the Service Discovery URL from the configuration ourselves and set it on the HttpClient.
            client.BaseAddress = new Uri(builder.Configuration["services:openfga:http:0"]!);

            // TODO For production, read store and model ids from configuration
        });
        
        services.AddTransient<OpenFgaMigrator>();
        services.AddTransient<IRebacAuthorizationService, OpenFgaAuthorizationService>();
    }
}
