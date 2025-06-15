using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Amolenk.Admitto.IntegrationTests.TestHelpers.Fixtures;

[TestClass]
public static class AssemblyTestFixture
{
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(30);

    public static DistributedApplication Application { get; private set; } = null!;
    public static IHost WorkerHost { get; private set; } = null!;
    public static AuthorizationTestFixture AuthorizationTestFixture { get; private set; } = null!;
    public static DatabaseTestFixture DatabaseTestFixture { get; private set; } = null!;
    public static EmailTestFixture EmailTestFixture { get; private set; } = null!;
    public static IdentityTestFixture IdentityTestFixture { get; private set; } = null!;
    public static QueueStorageTestFixture QueueStorageTestFixture { get; private set; } = null!;

    [AssemblyInitialize]
    public static async ValueTask AssemblyInitialize(TestContext testContext)
    {
        var cancellationToken = testContext.CancellationTokenSource.Token;
        
        var appHost = new TestingAspireAppHost();
        await appHost.StartAsync(cancellationToken).WaitAsync(DefaultTimeout, cancellationToken);
        
        await appHost.Application.ResourceNotifications
            .WaitForResourceAsync("api", KnownResourceStates.Running, cancellationToken)
            .WaitAsync(DefaultTimeout, cancellationToken);

        Application = appHost.Application;
        AuthorizationTestFixture = AuthorizationTestFixture.Create(appHost);
        DatabaseTestFixture = await DatabaseTestFixture.CreateAsync(appHost, cancellationToken);
        EmailTestFixture = EmailTestFixture.Create(appHost);
        IdentityTestFixture = IdentityTestFixture.Create(appHost);
        QueueStorageTestFixture = await QueueStorageTestFixture.CreateAsync(appHost);

        WorkerHost = await CreateWorkerHostAsync();
    }
    
    [AssemblyCleanup]
    public static async ValueTask AssemblyCleanup()
    {
        if (Application is IAsyncDisposable asyncDisposable)
        {
            await asyncDisposable.DisposeAsync();
        }
    }

    private static async ValueTask<IHost> CreateWorkerHostAsync()
    {
        var queuesConnectionString = await Application.GetConnectionStringAsync("queues");
        var keycloakEndpoint = AssemblyTestFixture.Application.GetEndpoint("keycloak", "http")
            .ToString();
        
        var builder = Host.CreateApplicationBuilder();

        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:queues"] = queuesConnectionString,
            ["Services:keycloak:http:0"] = keycloakEndpoint
        });
        
        builder.AddServiceDefaults();
        builder.Services.AddDefaultApplicationServices();
        builder.Services.AddCommandHandlers();
        builder.Services.AddEventualDomainEventHandlers();
        builder.AddDefaultInfrastructureServices();
        builder.AddSmtpEmailServices();

        return builder.Build();
    }
}
