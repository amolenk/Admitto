using Amolenk.Admitto.Core.Organization;
using Amolenk.Admitto.Core.Organization.Application.Bootstrap;
using Amolenk.Admitto.Core.Organization.Application.Persistence;
using Amolenk.Admitto.Core.Organization.Application.Services;
using Amolenk.Admitto.Core.Organization.Infrastructure.Persistence;
using Amolenk.Admitto.Core.Shared.Application.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace Amolenk.Admitto.Core.IntegrationTests.Organization.Application.Bootstrap;

internal sealed class BootstrapAdminInitializerFixture
{
    public const string AdminEmail = "bootstrap-admin@example.com";
    public const string FakeExternalUserId = "auth0|bootstrapped123";

    public IExternalUserDirectory ExternalUserDirectory { get; } = Substitute.For<IExternalUserDirectory>();

    public BootstrapAdminInitializer CreateInitializer(IntegrationTestEnvironment environment)
    {
        ExternalUserDirectory
            .InviteUserAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(FakeExternalUserId));

        var services = new ServiceCollection();

        services.AddSingleton<IOrganizationWriteStore>(
            environment.OrganizationDatabase.Context);

        services.AddKeyedSingleton<IUnitOfWork>(
            OrganizationModule.Key,
            (_, _) => (IUnitOfWork)new DbContextUnitOfWork(environment.OrganizationDatabase.Context));

        services.AddSingleton(ExternalUserDirectory);

        var serviceProvider = services.BuildServiceProvider();
        var scopeFactory = serviceProvider.GetRequiredService<IServiceScopeFactory>();

        var bootstrapOptions = Options.Create(
            new BootstrapAdminOptions { EmailAddress = AdminEmail });

        return new BootstrapAdminInitializer(
            scopeFactory,
            bootstrapOptions,
            NullLogger<BootstrapAdminInitializer>.Instance);
    }
}
