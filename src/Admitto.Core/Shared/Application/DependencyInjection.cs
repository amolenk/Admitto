using System.Reflection;
using Amolenk.Admitto.Core.Shared.Application.Cryptography;
using Amolenk.Admitto.Core.Shared.Application.Messaging;
using FluentValidation;
using FluentValidation.Internal;
using Humanizer;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Scrutor;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

public static class SharedApplicationExtensions
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddMessagingApplicationServices()
        {
            services.AddScoped<IMediator, Mediator>();

            return services;
        }

        public IServiceCollection AddCryptographyApplicationServices()
        {
            services.AddSingleton<ISigningService, SigningService>();

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
            HostCapability capabilities = HostCapability.None,
            Type? namespaceAnchor = null)
        {
            services.Scan(scan => scan
                .FromAssemblies(assembly)
                .AddClasses(
                    classes =>
                    {
                        var filtered = classes.AssignableTo(typeof(ICommandHandler<>));
                        if (namespaceAnchor is not null) filtered = filtered.InNamespaceOf(namespaceAnchor);
                        filtered.Where(c => MatchesCapabilities(c, capabilities));
                    },
                    publicOnly: false)
                .UsingRegistrationStrategy(new TryAddEnumerableStrategy())
                .As(t => t.GetInterfaces()
                    .Where(i => i.IsGenericType &&
                                (i.GetGenericTypeDefinition() == typeof(ICommandHandler<>)
                                 || i.GetGenericTypeDefinition() == typeof(ICommandHandler<,>))))
                .WithScopedLifetime());

            return services;
        }

        public IServiceCollection AddDomainEventHandlersFromAssembly(Assembly assembly, Type? namespaceAnchor = null)
        {
            services.Scan(scan => scan
                .FromAssemblies(assembly)
                .AddClasses(
                    classes =>
                    {
                        var filtered = classes.AssignableTo(typeof(IDomainEventHandler<>));
                        if (namespaceAnchor is not null) filtered = filtered.InNamespaceOf(namespaceAnchor);
                    },
                    publicOnly: false)
                .UsingRegistrationStrategy(new TryAddEnumerableStrategy())
                .As(t => t.GetInterfaces()
                    .Where(i => i.IsGenericType &&
                                i.GetGenericTypeDefinition() == typeof(IDomainEventHandler<>)))
                .WithScopedLifetime());

            return services;
        }

        public IServiceCollection AddIntegrationEventHandlersFromAssembly(
            Assembly assembly,
            string moduleKey,
            HostCapability capabilities = HostCapability.None,
            Type? namespaceAnchor = null)
        {
            services.Scan(scan => scan
                .FromAssemblies(assembly)
                .AddClasses(
                    classes =>
                    {
                        var filtered = classes.AssignableTo(typeof(IIntegrationEventHandler<>));
                        if (namespaceAnchor is not null) filtered = filtered.InNamespaceOf(namespaceAnchor);
                        filtered.Where(c => MatchesCapabilities(c, capabilities));
                    },
                    publicOnly: false)
                .UsingRegistrationStrategy(new TryAddEnumerableStrategy())
                .As(t => t.GetInterfaces()
                    .Where(i => i.IsGenericType &&
                                i.GetGenericTypeDefinition() == typeof(IIntegrationEventHandler<>)))
                .WithServiceKey(moduleKey)
                .WithScopedLifetime());

            // Add a marker service to identify that this module has integration event handlers.
            services.AddSingleton(new IntegrationEventSubscriber(moduleKey));

            return services;
        }

        public IServiceCollection AddQueryHandlersFromAssembly(
            Assembly assembly,
            HostCapability capabilities = HostCapability.None,
            Type? namespaceAnchor = null)
        {
            services.Scan(scan => scan
                .FromAssemblies(assembly)
                .AddClasses(
                    classes =>
                    {
                        var filtered = classes.AssignableTo<IQueryHandler>();
                        if (namespaceAnchor is not null) filtered = filtered.InNamespaceOf(namespaceAnchor);
                        filtered.Where(c => MatchesCapabilities(c, capabilities));
                    },
                    publicOnly: false)
                .UsingRegistrationStrategy(new TryAddEnumerableStrategy())
                .As(t => t.GetInterfaces()
                    .Where(i => i.IsGenericType &&
                                i.GetGenericTypeDefinition() == typeof(IQueryHandler<,>)))
                .WithScopedLifetime());

            return services;
        }

        private static bool MatchesCapabilities(Type handlerType, HostCapability capabilities)
        {
            var requiresCapabilityAttribute = handlerType.GetCustomAttribute<RequiresCapabilityAttribute>();
            return requiresCapabilityAttribute is null
                   || (requiresCapabilityAttribute.Capability & capabilities) ==
                   requiresCapabilityAttribute.Capability;
        }
    }

    private sealed class TryAddEnumerableStrategy : RegistrationStrategy
    {
        public override void Apply(IServiceCollection services, ServiceDescriptor descriptor)
        {
            services.TryAddEnumerable(descriptor);
        }
    }
}