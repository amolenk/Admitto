using System.Reflection;
using System.Text.Json;
using Amolenk.Admitto.Application.Abstractions;
using Amolenk.Admitto.Application.Data;
using Amolenk.Admitto.Application.MessageOutbox;
using Amolenk.Admitto.Infrastructure.Persistence;
using Amolenk.Admitto.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
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
        
        var connectionString = builder.Configuration.GetConnectionString("postgresDb");
        
        var infrastructureAssembly = Assembly.GetExecutingAssembly();
        
        var model = new ModelBuilder()
            .ApplyConfigurationsFromAssembly(infrastructureAssembly)
            .FinalizeModel();
        
        builder.Services.AddDbContextPool<ApplicationDbContext>(options => options
            .UseNpgsql(connectionString, b => b
                .EnableRetryOnFailure()
                .MigrationsAssembly(infrastructureAssembly))
            .UseModel(model));
        
        builder.EnrichNpgsqlDbContext<ApplicationDbContext>(settings =>
        {
            // Disable the configuration of the retry policy. It will override the NpgsqlDbContextOptions that we
            // need to set the migrations assembly. We've configured the retry policy in the UseNpgsql call.
            settings.DisableRetry = true;
        });

        builder.Services
            .AddScoped<IAttendeeRegistrationRepository, CosmosAttendeeRegistrationRepository>()
            .AddScoped<ITicketedEventRepository, CosmosTicketedEventRepository>();

        builder.Services
            .AddTransient<IOutboxMessageProvider, CosmosOutboxMessageProvider>();
        
        return builder;
    }
}