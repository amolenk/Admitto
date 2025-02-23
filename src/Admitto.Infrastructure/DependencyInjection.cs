using Amolenk.Admitto.Application.Abstractions;
using Amolenk.Admitto.Infrastructure.Persistence;
using Microsoft.Extensions.Hosting;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

public static class DependencyInjection
{
    public static IHostApplicationBuilder AddInfrastructureServices(this IHostApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        
        builder.AddCosmosDbContext<ApplicationDbContext>("cosmosdb", "Admitto");
        
        builder.Services
            .AddScoped<IAttendeeRepository, AttendeeRepository>()
            .AddScoped<ITicketedEventRepository, TicketedEventRepository>();
        
        return builder;
    }
}