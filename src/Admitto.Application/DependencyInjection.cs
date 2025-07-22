using System.Reflection;
using Amolenk.Admitto.Application;
using Amolenk.Admitto.Application.UseCases.Email;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

public static class DependencyInjection
{
    // TODO Split up further
    public static void AddDefaultApplicationServices(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

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
        // TODO If we add caching, we maybe get into issues with scoped lifetime
        services.AddScoped<IEmailTemplateService, EmailTemplateService>();

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