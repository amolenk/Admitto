using System.Configuration;
using Amolenk.Admitto.Application.Common.Abstractions;
using Amolenk.Admitto.Infrastructure.Persistence;
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

        builder.AddNpgsqlDbContext<ApplicationContext>(connectionName: "postgresdb");
        
        builder.Services
            .AddScoped<IApplicationContext, ApplicationContext>();
        
        var connectionString = builder.Configuration.GetConnectionString("postgresdb")!;
        
        builder.Services
            .AddTransient<PgOutboxMessageDispatcher>(sp => new PgOutboxMessageDispatcher(connectionString,
                sp.GetRequiredService<ILogger<PgOutboxMessageDispatcher>>()));
        
        return builder;
    }
}
