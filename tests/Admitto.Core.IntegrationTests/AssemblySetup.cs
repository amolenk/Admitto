namespace Amolenk.Admitto.Core.IntegrationTests;

[TestClass]
public static class AssemblySetup
{
    private static IntegrationTestAppHost? AppHost { get; set; }

    [AssemblyInitialize]
    public static async Task AssemblyInit(TestContext testContext)
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(2));

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
