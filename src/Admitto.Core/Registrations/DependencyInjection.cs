using System.Reflection;
using Amolenk.Admitto.Core.Registrations.Application;
using Amolenk.Admitto.Core.Registrations.Application.Common.Cryptography;
using Amolenk.Admitto.Core.Registrations.Application.Persistence;
using Amolenk.Admitto.Core.Registrations.Application.Security;
using Amolenk.Admitto.Core.Registrations.Application.UseCases;
using Amolenk.Admitto.Core.Registrations.Contracts;
using Amolenk.Admitto.Core.Registrations.Infrastructure.Persistence;
using Amolenk.Admitto.Core.Shared.Application.Cryptography;
using Amolenk.Admitto.Core.Shared.Infrastructure.Messaging;
using Amolenk.Admitto.Core.Shared.Infrastructure.Persistence;
using FluentValidation;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

public static class RegistrationsModuleExtensions
{
    public static IHostApplicationBuilder AddRegistrationsModule(this IHostApplicationBuilder builder)
    {
        var services = builder.Services;
        var configuration = builder.Configuration;
        var assembly = Assembly.GetExecutingAssembly();

        // Command handlers
        services.AddConcreteCommandHandlersFromAssembly(assembly, "Amolenk.Admitto.Core.Registrations");

        // Query handlers
        services.AddConcreteQueryHandlersFromAssembly(assembly, "Amolenk.Admitto.Core.Registrations");

        // Domain event handlers
        services.AddDomainEventHandlersFromAssembly(assembly, "Amolenk.Admitto.Core.Registrations");

        services.AddValidatorsFromAssembly(assembly);

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

    public static IHostApplicationBuilder AddRegistrationsModuleWorker(this IHostApplicationBuilder builder)
    {
        builder.Services.AddIntegrationEventHandlersFromAssembly(
            Assembly.GetExecutingAssembly(),
            "Amolenk.Admitto.Core.Registrations");

        return builder;
    }

    public static void AddRegistrationsMessageTypes(this MessageTypeRegistryBuilder builder)
    {
        builder.AddFromAssembly(Assembly.GetExecutingAssembly(), "Amolenk.Admitto.Core.Registrations");
    }
}
