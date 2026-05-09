using Amolenk.Admitto.Core.Module.Email.Tests.Application.Infrastructure;
using Amolenk.Admitto.Core.Module.Email.Tests.Application.Infrastructure.Hosting;
using Amolenk.Admitto.Core.Module.Organization.Tests.Application.Infrastructure;
using Amolenk.Admitto.Core.Module.Organization.Tests.Application.Infrastructure.Hosting;
using Amolenk.Admitto.Core.Module.Registrations.Tests.Application.Aspire;
using Amolenk.Admitto.Core.Module.Registrations.Tests.Application.Infrastructure.Hosting;

namespace Amolenk.Admitto.Core.Tests;

[TestClass]
public static class AssemblySetup
{
    private static Module.Organization.Tests.Application.Infrastructure.Hosting.IntegrationTestAppHost? AppHost { get; set; }

    [AssemblyInitialize]
    public static async Task AssemblyInit(TestContext testContext)
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

        AppHost = new Module.Organization.Tests.Application.Infrastructure.Hosting.IntegrationTestAppHost();
        await AppHost.StartAsync(cts.Token);

        await AppHost.Application.ResourceNotifications.WaitForResourceHealthyAsync(
            "admitto-db",
            cancellationToken: cts.Token);

        Module.Organization.Tests.Application.Infrastructure.AspireIntegrationTestBase.Environment =
            await Module.Organization.Tests.Application.Infrastructure.Hosting.IntegrationTestEnvironment.CreateAsync(AppHost, cts.Token);

        Module.Registrations.Tests.Application.Aspire.AspireIntegrationTestBase.Environment =
            await Module.Registrations.Tests.Application.Infrastructure.Hosting.IntegrationTestEnvironment.CreateAsync(AppHost, cts.Token);

        Module.Email.Tests.Application.Infrastructure.AspireIntegrationTestBase.Environment =
            await Module.Email.Tests.Application.Infrastructure.Hosting.IntegrationTestEnvironment.CreateAsync(AppHost, cts.Token);
    }

    [AssemblyCleanup]
    public static async Task AssemblyCleanup()
    {
        if (AppHost is not null)
            await AppHost.DisposeAsync();
    }
}
