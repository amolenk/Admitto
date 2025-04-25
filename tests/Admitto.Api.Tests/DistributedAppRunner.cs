using Amolenk.Admitto.Infrastructure.Persistence;
using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Amolenk.Admitto.Application.Tests;

[TestClass]
public static class DistributedAppRunner
{
    private static DistributedApplication _app = null!;
    private static DbContextOptions<ApplicationContext> _dbContextOptions = null!;

    [AssemblyInitialize]
    public static async ValueTask AssemblyInitialize(TestContext testContext)
    {
        // Start the distributed app.
        var appBuilder = await DistributedApplicationTestingBuilder.CreateAsync<Projects.Admitto_AppHost>(
        [
            "--environment Testing"
        ]);

        _app = await appBuilder.BuildAsync();

        await _app.StartAsync();

        // Get the connection string for the Postgres database.
        if (appBuilder.Resources.FirstOrDefault(r => r.Name == "postgresdb")
            is not PostgresDatabaseResource postgres)
        {
            throw new InvalidOperationException("Postgres database resource not found");
        }
        
        var connectionString = await postgres.ConnectionStringExpression.GetValueAsync(CancellationToken.None);

        // Create the DbContextOptions for the database.
        _dbContextOptions = new DbContextOptionsBuilder<ApplicationContext>()
            .UseNpgsql(connectionString)
            .Options;

        // Wait for the API to start.
        var resourceNotificationService = _app.Services.GetRequiredService<ResourceNotificationService>();
        await resourceNotificationService.WaitForResourceAsync("api", KnownResourceStates.Running)
            .WaitAsync(TimeSpan.FromSeconds(30));
    }

    [AssemblyCleanup]
    public static async ValueTask AssemblyCleanup()
    {
        await _app.DisposeAsync();
    }

    public static ApplicationContext CreateApplicationContext() => new(_dbContextOptions);

    public static HttpClient CreateApiClient() => _app.CreateHttpClient("api");
}