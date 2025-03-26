using System.Reflection;
using Amolenk.Admitto.Application;
using FluentValidation;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

public static class DependencyInjection
{
    public static void AddApplicationServices(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

        services.AddTransient<IEmailProvider, ConsoleEmailProvider>();
        
        // Register all command and domain event handlers
        services.Scan(scan => scan
            .FromAssemblyOf<ApplicationAssemblyLocator>()
            // Command handlers
            .AddClasses(classes => classes.AssignableTo<ICommandHandler>())
            .AsSelfWithInterfaces()
            .WithScopedLifetime()
            // Domain event handlers
            .AddClasses(classes => classes.AssignableTo<IDomainEventHandler>())
            .AsSelfWithInterfaces()
            .WithScopedLifetime()
            // Query handlers
            .AddClasses(classes => classes.AssignableTo<IQueryHandler>())
            .AsSelfWithInterfaces()
            .WithScopedLifetime());
    }
}