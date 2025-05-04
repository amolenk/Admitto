using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;

namespace Amolenk.Admitto.Application.Tests.Infrastructure;

[TestClass]
public static class GlobalAppHostFixture
{
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(30);

    private static TestingAspireAppHost _appHost = null!;

    public static DistributedApplication Application => _appHost.Application;

    [AssemblyInitialize]
    public static async ValueTask AssemblyInitialize(TestContext testContext)
    {
        
        var cancellationToken = testContext.CancellationTokenSource.Token;
        _appHost = new TestingAspireAppHost();
        
        await _appHost.StartAsync(cancellationToken).WaitAsync(DefaultTimeout, cancellationToken);

        // Wait for the API to start.
        await Application.ResourceNotifications.WaitForResourceAsync("api", KnownResourceStates.Running, cancellationToken)
            .WaitAsync(DefaultTimeout, cancellationToken);
    }
    
    [AssemblyCleanup]
    public static async ValueTask AssemblyCleanup()
    {
        await Application.DisposeAsync();
    }

    public static async Task<DatabaseFixture> GetDatabaseFixtureAsync(CancellationToken cancellationToken = default)
    {
        return await DatabaseFixture.CreateAsync(_appHost);
    }
}
