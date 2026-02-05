using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;

namespace Amolenk.Admitto.Testing.Infrastructure.Hosting;

public static class DistributedApplicationBuilderExtensions
{
    public static DistributedApplicationBuilder AddTestSuffixToVolumeMounts(
        this DistributedApplicationBuilder applicationBuilder)
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

                if (name.EndsWith("-test")) continue;

                resource.Annotations.Remove(volume);
                resource.Annotations.Add(
                    new ContainerMountAnnotation(
                        $"{name}-test",
                        volume.Target,
                        ContainerMountType.Volume,
                        volume.IsReadOnly));
            }
        }

        return applicationBuilder;
    }
}