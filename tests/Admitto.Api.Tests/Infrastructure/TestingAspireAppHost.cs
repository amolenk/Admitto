using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;

namespace Amolenk.Admitto.Application.Tests.Infrastructure;

public class TestingAspireAppHost()
    : DistributedApplicationFactory(typeof(Projects.Admitto_AppHost), ["DcpPublisher:ResourceNameSuffix=test"])
{
    public DistributedApplication Application { get; private set; } = null!;
    
    protected override void OnBuilding(DistributedApplicationBuilder applicationBuilder)
    {
        // Replace all volume mounts with the "-test" suffix.
        foreach (var resource in applicationBuilder.Resources)
        {
            var volumes = resource.Annotations
            .OfType<ContainerMountAnnotation>()
            .Where(m => m.Type == ContainerMountType.Volume && !string.IsNullOrEmpty(m.Source))
            .ToList();

            foreach (var volume in volumes)
            {
                var name = volume.Source!;

                if (!name.EndsWith("-test"))
                {
                    resource.Annotations.Remove(volume);
                    resource.Annotations.Add(new ContainerMountAnnotation(
                        $"{name}-test", volume.Target, ContainerMountType.Volume, volume.IsReadOnly));
                }
            }
        }
        
        base.OnBuilding(applicationBuilder);
    }

    protected override void OnBuilt(DistributedApplication application)
    {
        base.OnBuilt(application);
        Application = application;
    }
}