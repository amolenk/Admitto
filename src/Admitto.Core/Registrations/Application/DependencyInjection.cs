using System.Reflection;
using Amolenk.Admitto.Core.Registrations.Application.Common.Cryptography;
using Amolenk.Admitto.Core.Registrations.Application.Security;
using Amolenk.Admitto.Core.Registrations.Application.UseCases;
using Amolenk.Admitto.Core.Registrations.Contracts;
using Amolenk.Admitto.Core.Shared.Application.Cryptography;
using Amolenk.Admitto.Core.Shared.Infrastructure.Messaging;
using FluentValidation;
using Microsoft.Extensions.Hosting;

namespace Amolenk.Admitto.Core.Registrations.Application;

public static class DependencyInjection
{
    public static IHostApplicationBuilder AddRegistrationsModule(this IHostApplicationBuilder builder)
    {
        var services = builder.Services;
        var configuration = builder.Configuration;
        var executingAssembly = Assembly.GetExecutingAssembly();

        // Command handlers
        services.AddConcreteCommandHandlersFromAssembly(executingAssembly, "Amolenk.Admitto.Core.Registrations");

        // Query handlers
        services.AddConcreteQueryHandlersFromAssembly(executingAssembly, "Amolenk.Admitto.Core.Registrations");

        // Domain event handlers
        services.AddDomainEventHandlersFromAssembly(executingAssembly, "Amolenk.Admitto.Core.Registrations");

        services.AddValidatorsFromAssembly(executingAssembly);

        services.AddScoped<IRegistrationsFacade, RegistrationsFacade>();

        services.AddMemoryCache();
        services.AddScoped<IEventSigningKeyProvider, EventSigningKeyProvider>();
        services.AddScoped<RegistrationSigner>();

        services.Configure<VerificationTokenOptions>(
            configuration.GetSection(VerificationTokenOptions.SectionName));
        services.AddScoped<IVerificationTokenService, HmacVerificationTokenService>();

        services.Configure<OtpOptions>(
            configuration.GetSection(OtpOptions.SectionName));

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