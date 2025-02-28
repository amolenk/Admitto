using System.Text.Json;
using Amolenk.Admitto.Application.Abstractions;
using Amolenk.Admitto.Application.MessageOutbox;
using Amolenk.Admitto.Infrastructure.Persistence;
using Amolenk.Admitto.Infrastructure.Persistence.Repositories;
using Microsoft.Extensions.Hosting;

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
        
        builder.Services
            .AddScoped<IAttendeeRepository, CosmosAttendeeRepository>()
            .AddScoped<ITicketedEventRepository, CosmosTicketedEventRepository>();

        builder.Services
            .AddTransient<IOutboxMessageProvider, CosmosOutboxMessageProvider>();
        
        return builder;
    }
}