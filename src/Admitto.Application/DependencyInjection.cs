using System.Reflection;
using Amolenk.Admitto.Application;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

public static class DependencyInjection
{
    // TODO Split up further
    public static void AddDefaultApplicationServices(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

        // Register all command and domain event handlers
        services.Scan(scan => scan
            .FromAssemblyOf<ApplicationAssemblyLocator>()
            // Query handlers
            .AddClasses(classes => classes.AssignableTo<IQueryHandler>())
            .AsSelfWithInterfaces()
            .WithScopedLifetime());
        
        AddTransactionalDomainEventHandlers(services);
    }
    
    public static void AddCommandHandlers(this IServiceCollection services)
    {
        services.Scan(scan => scan
            .FromAssemblyOf<ApplicationAssemblyLocator>()
            .AddClasses(classes => classes.AssignableTo<ICommandHandler>())
            .AsSelfWithInterfaces()
            .WithScopedLifetime());
    }

    public static void AddJobHandlers(this IServiceCollection services)
    {
        services.Scan(scan => scan
            .FromAssemblyOf<ApplicationAssemblyLocator>()
            .AddClasses(classes => classes.AssignableTo<IJobHandler>())
            .AsSelfWithInterfaces()
            .WithScopedLifetime());
    }

    public static void AddTransactionalDomainEventHandlers(this IServiceCollection services)
    {
        services.Scan(scan => scan
            .FromAssemblyOf<ApplicationAssemblyLocator>()
            .AddClasses(classes => classes.AssignableTo<ITransactionalDomainEventHandler>())
            .AsSelfWithInterfaces()
            .WithScopedLifetime());
    }

    public static void AddEventualDomainEventHandlers(this IServiceCollection services)
    {
        services.Scan(scan => scan
            .FromAssemblyOf<ApplicationAssemblyLocator>()
            .AddClasses(classes => classes.AssignableTo<IEventualDomainEventHandler>())
            .AsSelfWithInterfaces()
            .WithScopedLifetime());
    }
}