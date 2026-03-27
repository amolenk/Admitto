using Amolenk.Admitto.Testing.Infrastructure.Hosting;
using Aspire.Hosting;
using Aspire.Hosting.Testing;
using Microsoft.Extensions.Hosting;

namespace Amolenk.Admitto.Module.Registrations.Tests.Application.Infrastructure.Hosting;

public sealed class IntegrationTestAppHost() : DistributedApplicationFactory(typeof(Projects.Admitto_AppHost))
{
    public DistributedApplication Application { get; private set; } = null!;

    protected override void OnBuilderCreating(
        DistributedApplicationOptions applicationOptions,
        HostApplicationBuilderSettings hostOptions)
    {
        hostOptions.Args = ["DcpPublisher:ResourceNameSuffix=test"];
        hostOptions.EnvironmentName = "IntegrationTesting";
    }

    protected override void OnBuilding(DistributedApplicationBuilder applicationBuilder)
    {
        applicationBuilder.AddTestSuffixToVolumeMounts();
    }

    protected override void OnBuilt(DistributedApplication application)
    {
        Application = application;
    }
}