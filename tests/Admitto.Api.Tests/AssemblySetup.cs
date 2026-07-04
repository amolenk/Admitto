using Amolenk.Admitto.Api.Tests.Infrastructure;
using Amolenk.Admitto.Api.Tests.Infrastructure.Hosting;
using Aspire.Hosting.ApplicationModel;

namespace Amolenk.Admitto.Api.Tests;

[TestClass]
public static class AspireTestAssemblySetup
{
    private static EndToEndTestAppHost? AppHost { get; set; }

    [AssemblyInitialize]
    public static async Task AssemblyInit(TestContext testContext)
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(120));

        AppHost = new EndToEndTestAppHost();
        await AppHost.StartAsync(cts.Token);
        
        await AppHost.Application.ResourceNotifications.WaitForResourceHealthyAsync(
            "api",
            cancellationToken: cts.Token);

        await AppHost.Application.ResourceNotifications.WaitForResourceAsync(
            "worker",
            KnownResourceStates.Running,
            cancellationToken: cts.Token);
        
        EndToEndTestBase.Environment = await EndToEndTestEnvironment.CreateAsync(AppHost, cts.Token);
    }

    [AssemblyCleanup]
    public static async Task AssemblyCleanup()
    {
        if (AppHost is not null)
            await AppHost.DisposeAsync();
    }
}