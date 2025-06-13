using Amolenk.Admitto.Domain.ValueObjects;
using Amolenk.Admitto.Infrastructure.Auth;
using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Respawn;

namespace Amolenk.Admitto.Application.Tests.TestFixtures;

[TestClass]
public static class GlobalAppHostFixture
{
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(30);

    private static TestingAspireAppHost _appHost = null!;
    private static Respawner _respawner = null!;

    // TODO Remove
    private static DistributedApplication Application => _appHost.Application;
    
    public static IConfiguration Configuration { get; private set; } = null!;

    [AssemblyInitialize]
    public static async ValueTask AssemblyInitialize(TestContext testContext)
    {
        var cancellationToken = testContext.CancellationTokenSource.Token;
        _appHost = new TestingAspireAppHost();

        await _appHost.StartAsync(cancellationToken).WaitAsync(DefaultTimeout, cancellationToken);
        
        await Application.ResourceNotifications
            .WaitForResourceAsync("api", KnownResourceStates.Running, cancellationToken)
            .WaitAsync(DefaultTimeout, cancellationToken);

        Configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json")
            .Build();
        
        _respawner = await CreateRespawnerAsync(cancellationToken);
    }
    
    [AssemblyCleanup]
    public static async ValueTask AssemblyCleanup()
    {
        await Application.DisposeAsync();
    }

    public static HttpClient GetApiClient()
    {
        var httpClient = Application.Services.GetRequiredService<IHttpClientFactory>()
            .CreateClient("AdmittoApi");
        
        httpClient.BaseAddress = Application.GetEndpoint("api");
        httpClient.Timeout = TimeSpan.FromSeconds(10);
        
        return httpClient;
    }
    
    public static async Task<DatabaseFixture> GetDatabaseFixtureAsync()
    {
        return await DatabaseFixture.CreateAsync(_appHost, _respawner);
    }

    public static EmailSettings GetDefaultEmailSettings() => new EmailSettings(
        "test@example.com",
        _appHost.Application.GetEndpoint("maildev", "http").Host,
        _appHost.Application.GetEndpoint("maildev", "http").Port);

    public static IdentityFixture GetIdentityFixture()
    {
        var httpClient = Application.Services.GetRequiredService<IHttpClientFactory>()
            .CreateClient("KeycloakApi");
        
        httpClient.BaseAddress = Application.GetEndpoint("keycloak");

        var identityService = new KeycloakIdentityService(httpClient);
        
        return new IdentityFixture(identityService);
    }

    public static AuthorizationFixture GetAuthorizationFixture()
    {
        var httpClient = _appHost.CreateHttpClient("openfga", "http");

        var clientFactory = new OpenFgaClientFactory(httpClient);
        var authorizationService = new OpenFgaAuthorizationService(clientFactory);
        
        return new AuthorizationFixture(authorizationService);
    }
    
    public static async ValueTask<QueueStorageFixture> GetQueueStorageFixtureAsync()
    {
        return await QueueStorageFixture.CreateAsync(_appHost);
    }

    private static async Task<Respawner> CreateRespawnerAsync(CancellationToken cancellationToken)
    {
        var connectionString = await _appHost.GetConnectionString("admitto-db");
        if (connectionString is null)
        {
            throw new InvalidOperationException(
                "Connection string for PostgreSQL database not found.");
        }

        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        return await Respawner.CreateAsync(connection, new RespawnerOptions
        {
            SchemasToInclude = [ "public" ],
            TablesToIgnore = [ "__EFMigrationsHistory" ],
            DbAdapter = DbAdapter.Postgres
        });
    }
}
