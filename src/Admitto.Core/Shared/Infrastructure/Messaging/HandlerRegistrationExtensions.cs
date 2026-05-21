using System.Reflection;
using Amolenk.Admitto.Core.Shared.Application.Messaging;
using FluentValidation;

namespace Amolenk.Admitto.Core.Shared.Infrastructure.Messaging;

/// <summary>
/// Assembly-scanning registration helpers.
/// </summary>
public static class HandlerRegistrationExtensions
{
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Scans for command handlers in <paramref name="namespacePrefix"/>.*
        /// </summary>
        public IServiceCollection AddCommandHandlersFromAssembly(
            Assembly assembly,
            string namespacePrefix,
            bool includeWorkerHandlers = false)
        {
            services.RegisterHandlers(assembly, namespacePrefix, typeof(ICommandHandler<>), includeWorkerHandlers);
            services.RegisterHandlers(assembly, namespacePrefix, typeof(ICommandHandler<,>), includeWorkerHandlers);
            return services;
        }

        /// <summary>
        /// Scans for query handlers in <paramref name="namespacePrefix"/>.*
        /// </summary>
        public IServiceCollection AddQueryHandlersFromAssembly(
            Assembly assembly,
            string namespacePrefix,
            bool includeWorkerHandlers = false)
        {
            services.RegisterHandlers(assembly, namespacePrefix, typeof(IQueryHandler<,>), includeWorkerHandlers);
            return services;
        }

        /// <summary>
        /// Scans for domain event handlers in <paramref name="namespacePrefix"/>.*
        /// </summary>
        public IServiceCollection AddDomainEventHandlersFromAssembly(
            Assembly assembly,
            string namespacePrefix)
        {
            services.RegisterHandlers(assembly, namespacePrefix, typeof(IDomainEventHandler<>));
            return services;
        }

        /// <summary>
        /// Scans for integration event handlers in <paramref name="namespacePrefix"/>.*
        /// </summary>
        public IServiceCollection AddIntegrationEventHandlersFromAssembly(
            Assembly assembly,
            string namespacePrefix)
        {
            services.RegisterHandlers(assembly, namespacePrefix, typeof(IIntegrationEventHandler<>));
            return services;
        }

        /// <summary>
        /// Scans for FluentValidation validators in <paramref name="namespacePrefix"/>.*
        /// only, restricting the scan to the owning module's namespace so that modules
        /// sharing a single assembly do not register each other's validators.
        /// </summary>
        public IServiceCollection AddValidatorsFromAssembly(
            Assembly assembly,
            string namespacePrefix)
        {
            return services.AddValidatorsFromAssembly(
                assembly,
                filter: result => result.ValidatorType.Namespace is not null
                                  && (result.ValidatorType.Namespace == namespacePrefix
                                      || result.ValidatorType.Namespace.StartsWith(
                                          namespacePrefix + ".",
                                          StringComparison.Ordinal)));
        }

        private void RegisterHandlers(
            Assembly assembly,
            string namespacePrefix,
            Type openHandlerType,
            bool includeWorkerHandlers = false)
        {
            var filter = new Func<Type, bool>(t => includeWorkerHandlers || !t.IsAssignableTo(typeof(IWorkerOnly)));

            foreach (var (closedInterface, implementation) in FindHandlers(assembly, namespacePrefix, openHandlerType))
            {
                if (filter is not null && !filter(implementation)) continue;

                services.AddScoped(closedInterface, implementation);
            }
        }
    }

    private static IEnumerable<(Type ClosedInterface, Type Implementation)> FindHandlers(
        Assembly assembly,
        string namespacePrefix,
        Type openHandlerType)
    {
        return assembly.GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false, Namespace: not null }
                        && (t.Namespace == namespacePrefix || t.Namespace.StartsWith(
                            namespacePrefix + ".",
                            StringComparison.Ordinal)))
            .SelectMany(t => t.GetInterfaces()
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == openHandlerType)
                .Select(i => (ClosedInterface: i, Implementation: t)));
    }
}
