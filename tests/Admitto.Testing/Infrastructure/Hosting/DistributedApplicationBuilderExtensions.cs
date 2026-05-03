using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;

namespace Amolenk.Admitto.Testing.Infrastructure.Hosting;

public static class DistributedApplicationBuilderExtensions
{
    // public static DistributedApplicationBuilder AddTestSuffixToVolumeMounts(
    //     this DistributedApplicationBuilder applicationBuilder)
    // {
    //     // Replace all volume mounts with the "-test" suffix.
    //     foreach (var resource in applicationBuilder.Resources)
    //     {
    //         var volumes = resource.Annotations
    //             .OfType<ContainerMountAnnotation>()
    //             .Where(m => m.Type == ContainerMountType.Volume && !string.IsNullOrEmpty(m.Source))
    //             .ToList();

    //         foreach (var volume in volumes)
    //         {
    //             var name = volume.Source!;

    //             if (name.EndsWith("-test")) continue;

    //             resource.Annotations.Remove(volume);
    //             resource.Annotations.Add(
    //                 new ContainerMountAnnotation(
    //                     $"{name}-test",
    //                     volume.Target,
    //                     ContainerMountType.Volume,
    //                     volume.IsReadOnly));
    //         }
    //     }

    //     return applicationBuilder;
    // }

    public static DistributedApplicationBuilder ReplaceVolumeMounts(this DistributedApplicationBuilder applicationBuilder)
    {
        // Replace all named volume mounts with anonymous volumes so data does not persist between test runs.
        foreach (var resource in applicationBuilder.Resources)
        {
            var volumes = resource.Annotations
                .OfType<ContainerMountAnnotation>()
                .Where(m => m.Type == ContainerMountType.Volume && !string.IsNullOrEmpty(m.Source))
                .ToList();

            foreach (var volume in volumes)
            {
                resource.Annotations.Remove(volume);
                resource.Annotations.Add(new ContainerMountAnnotation(
                    null, volume.Target, ContainerMountType.Volume, volume.IsReadOnly));
            }
        }

        return applicationBuilder;
    }

    public static DistributedApplicationBuilder ResetContainerLifetimes(this DistributedApplicationBuilder applicationBuilder)
    {
        // Remove Persistent lifetime annotations so containers use the default Session lifetime,
        // meaning they are stopped and removed after each test run.
        foreach (var resource in applicationBuilder.Resources)
        {
            var persistentLifetimes = resource.Annotations
                .OfType<ContainerLifetimeAnnotation>()
                .Where(a => a.Lifetime == ContainerLifetime.Persistent)
                .ToList();

            foreach (var annotation in persistentLifetimes)
            {
                resource.Annotations.Remove(annotation);
            }
        }

        return applicationBuilder;
    }
}