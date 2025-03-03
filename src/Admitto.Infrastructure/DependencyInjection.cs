using System.Text.Json;
using Amolenk.Admitto.Application.Abstractions;
using Amolenk.Admitto.Infrastructure.Persistence;
using Amolenk.Admitto.Infrastructure.Persistence.Repositories;
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

        builder.AddAzureCosmosClient("cosmos-db", configureClientOptions: options =>
        {
            options.UseSystemTextJsonSerializerWithOptions = JsonSerializerOptions.Web;
        });
        
        builder.AddNpgsqlDbContext<ApplicationDbContext>(connectionName: "postgresdb");
        

        builder.Services
            .AddScoped<IApplicationDbContext, ApplicationDbContext>();
        
        builder.Services
            .AddScoped<IAttendeeRegistrationRepository, CosmosAttendeeRegistrationRepository>()
            .AddScoped<ITicketedEventRepository, CosmosTicketedEventRepository>();

        var connectionString = builder.Configuration.GetConnectionString("postgresdb")!;
        
        builder.Services
            .AddTransient<PgOutboxMessageDispatcher>(sp => new PgOutboxMessageDispatcher(connectionString,
                sp.GetRequiredService<ILogger<PgOutboxMessageDispatcher>>()));
        
        return builder;
    }
}