using Amolenk.Admitto.Core.Email.Tests.Application.Infrastructure;
using Amolenk.Admitto.Core.Email.Tests.Application.Infrastructure.Hosting;
using Amolenk.Admitto.Core.Organization.Tests.Application.Infrastructure;
using Amolenk.Admitto.Core.Organization.Tests.Application.Infrastructure.Hosting;
using Amolenk.Admitto.Core.Registrations.Tests.Application.Aspire;
using Amolenk.Admitto.Core.Registrations.Tests.Application.Infrastructure.Hosting;

namespace Amolenk.Admitto.Core.Tests;

[TestClass]
public static class AssemblySetup
{
    private static Organization.Tests.Application.Infrastructure.Hosting.IntegrationTestAppHost? AppHost { get; set; }

    [AssemblyInitialize]
    public static async Task AssemblyInit(TestContext testContext)
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

        AppHost = new Organization.Tests.Application.Infrastructure.Hosting.IntegrationTestAppHost();
        await AppHost.StartAsync(cts.Token);

        await AppHost.Application.ResourceNotifications.WaitForResourceHealthyAsync(
            "admitto-db",
            cancellationToken: cts.Token);

        Organization.Tests.Application.Infrastructure.AspireIntegrationTestBase.Environment =
            await Organization.Tests.Application.Infrastructure.Hosting.IntegrationTestEnvironment.CreateAsync(AppHost, cts.Token);

        Registrations.Tests.Application.Aspire.AspireIntegrationTestBase.Environment =
            await Registrations.Tests.Application.Infrastructure.Hosting.IntegrationTestEnvironment.CreateAsync(AppHost, cts.Token);

        Email.Tests.Application.Infrastructure.AspireIntegrationTestBase.Environment =
            await Email.Tests.Application.Infrastructure.Hosting.IntegrationTestEnvironment.CreateAsync(AppHost, cts.Token);
    }

    [AssemblyCleanup]
    public static async Task AssemblyCleanup()
    {
        if (AppHost is not null)
            await AppHost.DisposeAsync();
    }
}
