using System.Reflection;
using Amolenk.Admitto.Core.Registrations;
using Amolenk.Admitto.Core.Registrations.Application.Common.Cryptography;
using Amolenk.Admitto.Core.Registrations.Application.Persistence;
using Amolenk.Admitto.Core.Registrations.Application.Security;
using Amolenk.Admitto.Core.Registrations.Application.UseCases;
using Amolenk.Admitto.Core.Registrations.Contracts;
using Amolenk.Admitto.Core.Registrations.Infrastructure.Persistence;
using Amolenk.Admitto.Core.Shared.Application.Cryptography;
using Amolenk.Admitto.Core.Shared.Infrastructure.Messaging;
using Amolenk.Admitto.Core.Shared.Infrastructure.Persistence;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

public static class RegistrationsModuleExtensions
{
    extension(IHostApplicationBuilder builder)
    {
        public IHostApplicationBuilder AddRegistrationsModule(bool includeWorkerHandlers = false)
        {
            var services = builder.Services;
            var configuration = builder.Configuration;
            var assembly = Assembly.GetExecutingAssembly();

            // Command handlers
            services.AddCommandHandlersFromAssembly(
                assembly,
                RegistrationsModule.NamespacePrefix,
                includeWorkerHandlers);

            // Query handlers
            services.AddQueryHandlersFromAssembly(assembly, RegistrationsModule.NamespacePrefix);

            // Domain event handlers
            services.AddDomainEventHandlersFromAssembly(assembly, RegistrationsModule.NamespacePrefix);

            services.AddValidatorsFromAssembly(assembly, RegistrationsModule.NamespacePrefix);

            // Message type registry contribution
            services.AddSingleton<Action<MessageTypeRegistryBuilder>>(b => b.AddFromAssembly(
                assembly,
                RegistrationsModule.NamespacePrefix));

            services.AddScoped<IRegistrationsFacade, RegistrationsFacade>();

            services.AddMemoryCache();
            services.AddScoped<IEventSigningKeyProvider, EventSigningKeyProvider>();
            services.AddScoped<RegistrationSigner>();

            services.Configure<VerificationTokenOptions>(
                configuration.GetSection(VerificationTokenOptions.SectionName));
            services.AddScoped<IVerificationTokenService, HmacVerificationTokenService>();

            services.Configure<OtpOptions>(
                configuration.GetSection(OtpOptions.SectionName));

            // Infrastructure
            builder.AddModuleDatabaseServices<IRegistrationsWriteStore, RegistrationsDbContext>(
                RegistrationsModule.Key);

            services.AddKeyedScoped<IPostgresExceptionMapping, RegistrationsPostgresExceptionMapping>(
                RegistrationsModule.Key);

            return builder;
        }

        public IHostApplicationBuilder AddRegistrationsModuleWorker()
        {
            builder.AddRegistrationsModule(includeWorkerHandlers: true);

            builder.Services.AddIntegrationEventHandlersFromAssembly(
                Assembly.GetExecutingAssembly(),
                RegistrationsModule.NamespacePrefix);

            return builder;
        }
    }
}
