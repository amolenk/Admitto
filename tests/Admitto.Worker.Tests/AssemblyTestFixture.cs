using Amolenk.Admitto.TestHelpers;
using Amolenk.Admitto.TestHelpers.TestFixtures;
using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;

namespace Amolenk.Admitto.Worker.Tests;

public static class AssemblyTestFixture
{
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(30);

    public static DistributedApplication Application { get; private set; } = null!;
    public static AuthorizationTestFixture AuthorizationFixture { get; private set; } = null!;
    public static DatabaseTestFixture DatabaseFixture { get; private set; } = null!;
    public static EmailTestFixture EmailFixture { get; private set; } = null!;
    public static IdentityTestFixture IdentityFixture { get; private set; } = null!;
    public static QueueStorageTestFixture QueueStorageFixture { get; private set; } = null!;

    [AssemblyInitialize]
    public static async ValueTask AssemblyInitialize(TestContext testContext)
    {
        var cancellationToken = testContext.CancellationTokenSource.Token;
        
        var appHost = new TestingAspireAppHost();
        await appHost.StartAsync(cancellationToken).WaitAsync(DefaultTimeout, cancellationToken);
        
        await appHost.Application.ResourceNotifications
            .WaitForResourceAsync("worker", KnownResourceStates.Running, cancellationToken)
            .WaitAsync(DefaultTimeout, cancellationToken);

        Application = appHost.Application;
        AuthorizationFixture = AuthorizationTestFixture.Create(appHost);
        DatabaseFixture = await DatabaseTestFixture.CreateAsync(appHost, cancellationToken);
        EmailFixture = EmailTestFixture.Create(appHost);
        IdentityFixture = IdentityTestFixture.Create(appHost);
        QueueStorageFixture = await QueueStorageTestFixture.CreateAsync(appHost);
    }
    
    [AssemblyCleanup]
    public static async ValueTask AssemblyCleanup()
    {
        await Application.DisposeAsync();
    }
}