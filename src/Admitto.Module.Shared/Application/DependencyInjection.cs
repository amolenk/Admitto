using System.Reflection;
using Amolenk.Admitto.Module.Shared.Application.Messaging;
using FluentValidation;
using FluentValidation.Internal;
using Humanizer;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

public static class DependencyInjection
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddMessagingApplicationServices()
        {
            services.AddScoped<IMediator, Mediator>();

            return services;
        }

        public IServiceCollection AddValidationApplicationServices()
        {
            // Use camel case for FluentValidation property names
            ValidatorOptions.Global.DisplayNameResolver = (_, member, _) => member?.Name.Humanize();
            ValidatorOptions.Global.PropertyNameResolver = (_, memberInfo, expression) =>
            {
                if (expression != null)
                {
                    var chain = PropertyChain.FromExpression(expression);
                    if (chain.Count > 0)
                    {
                        var propertyNames = chain.ToString().Split(ValidatorOptions.Global.PropertyChainSeparator);
                        if (propertyNames.Length == 1)
                        {
                            return propertyNames[0].Camelize();
                        }

                        return string.Join(
                            ValidatorOptions.Global.PropertyChainSeparator,
                            propertyNames.Select(n => n.Camelize()));
                    }
                }

                return memberInfo?.Name.Camelize();
            };

            return services;
        }

        public IServiceCollection AddCommandHandlersFromAssembly(
            Assembly assembly,
            HostCapability capabilities = HostCapability.None)
        {
            services.Scan(scan => scan
                .FromAssemblies(assembly)
                .AddClasses(
                    classes => classes
                        .AssignableTo<ICommandHandler>()
                        .Where(c =>
                        {
                            var requiresCapabilityAttribute = c.GetCustomAttribute<RequiresCapabilityAttribute>();
                            return requiresCapabilityAttribute is null
                                   || (requiresCapabilityAttribute.Capability & capabilities) ==
                                   requiresCapabilityAttribute.Capability;
                        }),
                    publicOnly: false)
                .As(t => t.GetInterfaces()
                    .Where(i => i.IsGenericType &&
                                (i.GetGenericTypeDefinition() == typeof(ICommandHandler<>)
                                 || i.GetGenericTypeDefinition() == typeof(ICommandHandler<,>))))
                .WithScopedLifetime());

            return services;
        }

        public IServiceCollection AddDomainEventHandlersFromAssembly(Assembly assembly)
        {
            services.Scan(scan => scan
                .FromAssemblies(assembly)
                .AddClasses(
                    classes => classes.AssignableTo(typeof(IDomainEventHandler<>)),
                    publicOnly: false)
                .As(t => t.GetInterfaces()
                    .Where(i => i.IsGenericType &&
                                i.GetGenericTypeDefinition() == typeof(IDomainEventHandler<>)))
                .WithScopedLifetime());

            return services;
        }

        public IServiceCollection AddModuleEventHandlersFromAssembly(Assembly assembly)
        {
            services.Scan(scan => scan
                .FromAssemblies(assembly)
                .AddClasses(
                    classes => classes.AssignableTo(typeof(IModuleEventHandler<>)),
                    publicOnly: false)
                .As(t => t.GetInterfaces()
                    .Where(i => i.IsGenericType &&
                                i.GetGenericTypeDefinition() == typeof(IModuleEventHandler<>)))
                .WithScopedLifetime());

            return services;
        }

        public IServiceCollection AddIntegrationEventHandlersFromAssembly(Assembly assembly, string moduleKey)
        {
            services.Scan(scan => scan
                .FromAssemblies(assembly)
                .AddClasses(
                    classes => classes.AssignableTo(typeof(IIntegrationEventHandler<>)),
                    publicOnly: false)
                .As(t => t.GetInterfaces()
                    .Where(i => i.IsGenericType &&
                                i.GetGenericTypeDefinition() == typeof(IIntegrationEventHandler<>)))
                .WithServiceKey(moduleKey)
                .WithScopedLifetime());

            // Add a marker service to identify that this module has integration event handlers.
            services.AddSingleton(new IntegrationEventSubscriber(moduleKey));

            return services;
        }

        public IServiceCollection AddQueryHandlersFromAssembly(Assembly assembly)
        {
            services.Scan(scan => scan
                .FromAssemblies(assembly)
                .AddClasses(
                    classes => classes.AssignableTo<IQueryHandler>(),
                    publicOnly: false)
                .As(t => t.GetInterfaces()
                    .Where(i => i.IsGenericType &&
                                i.GetGenericTypeDefinition() == typeof(IQueryHandler<,>)))
                .WithScopedLifetime());

            return services;
        }
    }
}