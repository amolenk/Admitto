using Amolenk.Admitto.Core.Organization;
using Amolenk.Admitto.Core.Organization.Application.ExternalUsers;
using Amolenk.Admitto.Core.Organization.Application.Persistence;
using Amolenk.Admitto.Core.Organization.Application.UseCases.TeamMemberships.BootstrapAdminUser;
using Amolenk.Admitto.Core.Shared.Application.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace Amolenk.Admitto.Core.IntegrationTests.Organization.Application.UseCases.TeamMemberships.BootstrapAdminUser;

internal sealed class BootstrapAdminUserInitializerFixture
{
    public const string AdminEmail = "bootstrap-admin@example.com";
    public const string ExternalUserId = "keycloak-user-id";

    private BootstrapAdminUserInitializerFixture(IntegrationTestEnvironment environment)
    {
        var services = new ServiceCollection();

        ExternalUserDirectory.InviteUserAsync(AdminEmail, Arg.Any<CancellationToken>())
            .Returns(ExternalUserId);

        services.AddSingleton<IOrganizationWriteStore>(
            environment.OrganizationDatabase.Context);
        services.AddSingleton(ExternalUserDirectory);

        services.AddKeyedSingleton<IUnitOfWork>(
            OrganizationModule.Key,
            (_, _) => new DbContextUnitOfWork(environment.OrganizationDatabase.Context));

        var serviceProvider = services.BuildServiceProvider();
        var scopeFactory = serviceProvider.GetRequiredService<IServiceScopeFactory>();

        var bootstrapOptions = Options.Create(
            new BootstrapAdminUserOptions { EmailAddress = AdminEmail });

        Initializer = new BootstrapAdminUserInitializer(
            scopeFactory,
            bootstrapOptions,
            NullLogger<BootstrapAdminUserInitializer>.Instance);
    }

    public IExternalUserDirectory ExternalUserDirectory { get; } = Substitute.For<IExternalUserDirectory>();

    public BootstrapAdminUserInitializer Initializer { get; }

    public static BootstrapAdminUserInitializerFixture Create(IntegrationTestEnvironment environment) => new(environment);

    public static BootstrapAdminUserInitializer CreateInitializer(IntegrationTestEnvironment environment) =>
        Create(environment).Initializer;
}
