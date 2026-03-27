using Amolenk.Admitto.Module.Organization.Tests.Application.Infrastructure;
using Amolenk.Admitto.Module.Organization.Tests.Application.Infrastructure.Hosting;

namespace Amolenk.Admitto.Module.Organization.Tests.Application;

[TestClass]
public static class AssemblySetup
{
    private static IntegrationTestAppHost? AppHost { get; set; }

    [AssemblyInitialize]
    public static async Task AssemblyInit(TestContext textContext)
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

        AppHost = new IntegrationTestAppHost();
        await AppHost.StartAsync(cts.Token);
        
        await AppHost.Application.ResourceNotifications.WaitForResourceHealthyAsync(
            "admitto-db",
            cancellationToken: cts.Token);

        AspireIntegrationTestBase.Environment = await IntegrationTestEnvironment.CreateAsync(AppHost, cts.Token);
    }

    [AssemblyCleanup]
    public static async Task AssemblyCleanup()
    {
        if (AppHost is not null)
            await AppHost.DisposeAsync();
    }
}