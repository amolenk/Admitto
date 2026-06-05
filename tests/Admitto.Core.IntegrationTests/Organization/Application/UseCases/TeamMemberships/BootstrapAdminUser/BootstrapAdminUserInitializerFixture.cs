using Amolenk.Admitto.Core.Organization;
using Amolenk.Admitto.Core.Organization.Application.Persistence;
using Amolenk.Admitto.Core.Organization.Application.UseCases.TeamMemberships.BootstrapAdminUser;
using Amolenk.Admitto.Core.Shared.Application.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Amolenk.Admitto.Core.IntegrationTests.Organization.Application.UseCases.TeamMemberships.BootstrapAdminUser;

internal static class BootstrapAdminUserInitializerFixture
{
    public const string AdminEmail = "bootstrap-admin@example.com";

    public static BootstrapAdminUserInitializer CreateInitializer(IntegrationTestEnvironment environment)
    {
        var services = new ServiceCollection();

        services.AddSingleton<IOrganizationWriteStore>(
            environment.OrganizationDatabase.Context);

        services.AddKeyedSingleton<IUnitOfWork>(
            OrganizationModule.Key,
            (_, _) => new DbContextUnitOfWork(environment.OrganizationDatabase.Context));

        var serviceProvider = services.BuildServiceProvider();
        var scopeFactory = serviceProvider.GetRequiredService<IServiceScopeFactory>();

        var bootstrapOptions = Options.Create(
            new BootstrapAdminUserOptions { EmailAddress = AdminEmail });

        return new BootstrapAdminUserInitializer(
            scopeFactory,
            bootstrapOptions,
            NullLogger<BootstrapAdminUserInitializer>.Instance);
    }
}
