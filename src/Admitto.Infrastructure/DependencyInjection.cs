using Amolenk.Admitto.Application.Common.Abstractions;
using Amolenk.Admitto.Application.UseCases.Email;
using Amolenk.Admitto.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

public static class DependencyInjection
{
    public static IHostApplicationBuilder AddInfrastructureServices(this IHostApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        var connectionString = builder.Configuration.GetConnectionString("postgresdb")!;

        builder.Services.AddDbContext<ApplicationContext>((sp, options) =>
        {
            options.UseNpgsql(connectionString);
            options.AddInterceptors(new DomainEventsInterceptor(sp));
        });
        
        builder.EnrichNpgsqlDbContext<ApplicationContext>();
        
        builder.AddKeyedAzureQueueClient("queues");

        builder.Services
            .AddScoped<IDomainContext>(sp => sp.GetRequiredService<ApplicationContext>())
            .AddScoped<IReadModelContext>(sp => sp.GetRequiredService<ApplicationContext>())
            .AddScoped<IAuthContext>(sp => sp.GetRequiredService<ApplicationContext>())
            .AddScoped<IEmailContext>(sp => sp.GetRequiredService<ApplicationContext>())
            .AddScoped<IEmailOutbox>(sp => sp.GetRequiredService<ApplicationContext>())
            .AddScoped<IMessageOutbox>(sp => sp.GetRequiredService<ApplicationContext>())
            .AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<ApplicationContext>());
        
        // var connectionString = builder.Configuration.GetConnectionString("postgresdb")!;
        
        builder.Services
            .AddTransient<PgOutboxMessageProcessor>(sp => new PgOutboxMessageProcessor(connectionString,
                sp.GetRequiredService<ILogger<PgOutboxMessageProcessor>>()));
        
        return builder;
    }
}
