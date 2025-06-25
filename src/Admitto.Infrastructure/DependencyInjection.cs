using Amolenk.Admitto.Application.Common.Abstractions;
using Amolenk.Admitto.Application.UseCases.Email;
using Amolenk.Admitto.Infrastructure;
using Amolenk.Admitto.Infrastructure.Auth;
using Amolenk.Admitto.Infrastructure.Email;
using Amolenk.Admitto.Infrastructure.Messaging;
using Amolenk.Admitto.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

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
            .AddScoped<IEmailContext>(sp => sp.GetRequiredService<ApplicationContext>())
            .AddScoped<IProcessedMessageContext>(sp => sp.GetRequiredService<ApplicationContext>());
        
        builder.Services
            .AddScoped<MessageOutbox>()
            .AddScoped<IMessageOutbox>(sp => sp.GetRequiredService<MessageOutbox>());

        builder.Services.AddScoped<IEmailOutbox, EmailOutbox>();
        
        builder.Services.AddScoped<IExactlyOnceProcessor, ExactlyOnceProcessor>();
        
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
        builder.AddKeycloakServices();
        builder.AddOpenFgaServices();
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
