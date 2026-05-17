using System.Reflection;
using Amolenk.Admitto.Core.Shared.Application.Messaging;
using FluentValidation;

namespace Amolenk.Admitto.Core.Shared.Infrastructure.Messaging;

/// <summary>
/// Assembly-scanning registration helpers that replace per-type
/// <c>AddScoped</c> calls in each module's DI setup.
/// All modules live in a single assembly; the <paramref name="namespacePrefix"/>
/// limits each scan to the owning module's namespace.
/// </summary>
public static class HandlerRegistrationExtensions
{
    /// <summary>
    /// Scans for non-abstract concrete types in <paramref name="namespacePrefix"/>.*
    /// implementing any <c>ICommandHandler&lt;TCommand&gt;</c> or
    /// <c>ICommandHandler&lt;TCommand, TResult&gt;</c> variant and registers each as
    /// <c>AddScoped&lt;THandler&gt;()</c> (concrete only).
    /// For queue-dispatched commands and handlers that need <c>ICommandHandler&lt;T&gt;</c>
    /// by interface, add explicit mappings in the Worker-specific DI method.
    /// </summary>
    public static IServiceCollection AddConcreteCommandHandlersFromAssembly(
        this IServiceCollection services,
        Assembly assembly,
        string namespacePrefix)
    {
        var commandHandlerType = typeof(ICommandHandler<>);
        var commandHandlerWithResultType = typeof(ICommandHandler<,>);

        foreach (var type in assembly.GetTypes()
                     .Where(t => t is { IsClass: true, IsAbstract: false }
                                 && t.Namespace is not null
                                 && (t.Namespace == namespacePrefix
                                     || t.Namespace.StartsWith(namespacePrefix + ".", StringComparison.Ordinal))))
        {
            var hasCommandInterface = type.GetInterfaces()
                .Any(i => i.IsGenericType
                          && (i.GetGenericTypeDefinition() == commandHandlerType
                              || i.GetGenericTypeDefinition() == commandHandlerWithResultType));

            if (hasCommandInterface)
            {
                services.AddScoped(type);
            }
        }

        return services;
    }

    /// <summary>
    /// Scans for non-abstract concrete types in <paramref name="namespacePrefix"/>.*
    /// implementing any <c>IQueryHandler&lt;TQuery, TResult&gt;</c> variant and
    /// registers each as <c>AddScoped&lt;THandler&gt;()</c> (concrete only).
    /// </summary>
    public static IServiceCollection AddConcreteQueryHandlersFromAssembly(
        this IServiceCollection services,
        Assembly assembly,
        string namespacePrefix)
    {
        var handlerOpenType = typeof(IQueryHandler<,>);

        foreach (var (_, implementation) in FindHandlers(assembly, namespacePrefix, handlerOpenType))
        {
            services.AddScoped(implementation);
        }

        return services;
    }

    /// <summary>
    /// Scans for non-abstract concrete types in <paramref name="namespacePrefix"/>.*
    /// implementing closed <c>IDomainEventHandler&lt;TEvent&gt;</c> and registers
    /// each as <c>AddScoped&lt;IDomainEventHandler&lt;TEvent&gt;, THandler&gt;()</c>.
    /// </summary>
    public static IServiceCollection AddDomainEventHandlersFromAssembly(
        this IServiceCollection services,
        Assembly assembly,
        string namespacePrefix)
    {
        var handlerOpenType = typeof(IDomainEventHandler<>);

        foreach (var (closedInterface, implementation) in FindHandlers(assembly, namespacePrefix, handlerOpenType))
        {
            services.AddScoped(closedInterface, implementation);
        }

        return services;
    }

    /// <summary>
    /// Scans for non-abstract concrete types in <paramref name="namespacePrefix"/>.*
    /// implementing closed <c>IIntegrationEventHandler&lt;TEvent&gt;</c> and registers
    /// each as <c>AddScoped&lt;IIntegrationEventHandler&lt;TEvent&gt;, THandler&gt;()</c>.
    /// </summary>
    public static IServiceCollection AddIntegrationEventHandlersFromAssembly(
        this IServiceCollection services,
        Assembly assembly,
        string namespacePrefix)
    {
        var handlerOpenType = typeof(IIntegrationEventHandler<>);

        foreach (var (closedInterface, implementation) in FindHandlers(assembly, namespacePrefix, handlerOpenType))
        {
            services.AddScoped(closedInterface, implementation);
        }

        return services;
    }

    /// <summary>
    /// Scans for FluentValidation validators in <paramref name="namespacePrefix"/>.*
    /// only, restricting the scan to the owning module's namespace so that modules
    /// sharing a single assembly do not register each other's validators.
    /// </summary>
    public static IServiceCollection AddValidatorsFromAssembly(
        this IServiceCollection services,
        Assembly assembly,
        string namespacePrefix)
    {
        return services.AddValidatorsFromAssembly(assembly,
            filter: result => result.ValidatorType.Namespace is not null
                              && (result.ValidatorType.Namespace == namespacePrefix
                                  || result.ValidatorType.Namespace.StartsWith(
                                      namespacePrefix + ".", StringComparison.Ordinal)));
    }

    private static IEnumerable<(Type ClosedInterface, Type Implementation)> FindHandlers(
        Assembly assembly,
        string namespacePrefix,
        Type openHandlerType)
    {
        return assembly.GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false }
                        && t.Namespace is not null
                        && (t.Namespace == namespacePrefix || t.Namespace.StartsWith(namespacePrefix + ".", StringComparison.Ordinal)))
            .SelectMany(t => t.GetInterfaces()
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == openHandlerType)
                .Select(i => (ClosedInterface: i, Implementation: t)));
    }
}
