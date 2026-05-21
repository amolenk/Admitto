using System.Reflection;
using Amolenk.Admitto.Core.Badges;
using Amolenk.Admitto.Core.Badges.Application.Persistence;
using Amolenk.Admitto.Core.Badges.Infrastructure.Persistence;
using Amolenk.Admitto.Core.Shared.Infrastructure.Messaging;
using Amolenk.Admitto.Core.Shared.Infrastructure.Persistence;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

public static class BadgesModuleExtensions
{
    extension(IHostApplicationBuilder builder)
    {
        public IHostApplicationBuilder AddBadgesModule(bool includeWorkerHandlers = false)
        {
            var services = builder.Services;
            var assembly = Assembly.GetExecutingAssembly();

            // Command handlers
            services.AddCommandHandlersFromAssembly(assembly, BadgesModule.NamespacePrefix, includeWorkerHandlers);

            // Query handlers
            services.AddQueryHandlersFromAssembly(assembly, BadgesModule.NamespacePrefix, includeWorkerHandlers);

            // Domain event handlers
            services.AddDomainEventHandlersFromAssembly(assembly, BadgesModule.NamespacePrefix);

            // Validators
            services.AddValidatorsFromAssembly(assembly, BadgesModule.NamespacePrefix);

            // Message type registry contribution
            services.AddSingleton<Action<MessageTypeRegistryBuilder>>(b => b.AddFromAssembly(
                assembly,
                BadgesModule.NamespacePrefix));

            // Infrastructure
            builder.AddModuleDatabaseServices<IBadgesWriteStore, BadgesDbContext>(BadgesModule.Key);
            services.AddKeyedScoped<IPostgresExceptionMapping, BadgesPostgresExceptionMapping>(
                BadgesModule.Key);

            return builder;
        }

        public IHostApplicationBuilder AddBadgesModuleWorker()
        {
            builder.AddBadgesModule(includeWorkerHandlers: true);

            builder.Services.AddIntegrationEventHandlersFromAssembly(
                Assembly.GetExecutingAssembly(),
                BadgesModule.NamespacePrefix);

            return builder;
        }
    }
}
