using Aspire.Hosting.Azure;

namespace Admitto.AppHost.Extensions.AzureServiceBus;

public static class AzureServiceBusBuilderExtensions
{
    /// <summary>
    /// The emulator is currently using sql-edge which is not compatible with the latest Linux kernel and (at least on ARM macos)
    /// crashes on start when using recent docker versions. Sql-edge is not going to be supported anymore.
    /// See https://github.com/dotnet/aspire/issues/9279
    /// </summary>
    public static IResourceBuilder<T> ReplaceEmulatorDatabase<T>(this IResourceBuilder<T> builder)
        where T : AzureServiceBusResource
    {
        var applicationBuilder = builder.ApplicationBuilder;

        var sqlEdgeResourceName = $"{builder.Resource.Name}-sqledge";
        var sqlEdgeResource = applicationBuilder.Resources
            .FirstOrDefault(r => r is ContainerResource resource && resource.Name == sqlEdgeResourceName);

        if (sqlEdgeResource is null) return builder;
        
        applicationBuilder.Resources.Remove(sqlEdgeResource);

        var passwordAnnotation = sqlEdgeResource.Annotations.Last(a => a is EnvironmentCallbackAnnotation);

        applicationBuilder
            .AddContainer(
                sqlEdgeResourceName,
                image: "mcr.microsoft.com/mssql/server",
                tag: "2019-latest")
            .WithEndpoint(targetPort: 1433, name: "tcp")
            .WithEnvironment("ACCEPT_EULA", "Y")
            .WithEnvironment("MSSQL_PID", "Express")
            .WithAnnotation(passwordAnnotation)
            .WithLifetime(ContainerLifetime.Persistent)
            .WithParentRelationship(builder);

        return builder;
    }
}