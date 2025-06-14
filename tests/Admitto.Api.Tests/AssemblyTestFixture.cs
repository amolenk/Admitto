using Amolenk.Admitto.TestHelpers;
using Amolenk.Admitto.TestHelpers.TestFixtures;
using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;

namespace Amolenk.Admitto.Api.Tests;

public static class AssemblyTestFixture
{
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(30);

    public static DistributedApplication Application { get; private set; } = null!;
    public static AuthorizationTestFixture Authorization { get; private set; } = null!;
    public static DatabaseTestFixture Database { get; private set; } = null!;
    public static EmailTestFixture Email { get; private set; } = null!;
    public static IdentityTestFixture Identity { get; private set; } = null!;
    public static QueueStorageTestFixture QueueStorage { get; private set; } = null!;

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
        Authorization = AuthorizationTestFixture.Create(appHost);
        Database = await DatabaseTestFixture.CreateAsync(appHost, cancellationToken);
        Email = EmailTestFixture.Create(appHost);
        Identity = IdentityTestFixture.Create(appHost);
        QueueStorage = await QueueStorageTestFixture.CreateAsync(appHost);
    }
    
    [AssemblyCleanup]
    public static async ValueTask AssemblyCleanup()
    {
        await Application.DisposeAsync();
    }
}