using System.Reflection;
using Amolenk.Admitto.Module.Registrations.Application.Common.Cryptography;
using Amolenk.Admitto.Module.Registrations.Application.Messaging;
using Amolenk.Admitto.Module.Registrations.Application.Security;
using Amolenk.Admitto.Module.Registrations.Application.UseCases;
using Amolenk.Admitto.Module.Registrations.Contracts;
using Amolenk.Admitto.Module.Shared.Application.Cryptography;
using Amolenk.Admitto.Module.Shared.Application.Http;
using Amolenk.Admitto.Module.Shared.Application.Messaging;
using FluentValidation;

namespace Amolenk.Admitto.Module.Registrations.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddRegistrationsApplicationServices(
        this IServiceCollection services,
        HostCapability capabilities = HostCapability.None)
    {
        var executingAssembly = Assembly.GetExecutingAssembly();

        services.AddCommandHandlersFromAssembly(executingAssembly, capabilities);
        services.AddDomainEventHandlersFromAssembly(executingAssembly);
        services.AddModuleEventHandlersFromAssembly(executingAssembly, capabilities);
        services.AddIntegrationEventHandlersFromAssembly(executingAssembly, RegistrationsModule.Key, capabilities);
        services.AddQueryHandlersFromAssembly(executingAssembly, capabilities);
        services.AddValidatorsFromAssembly(executingAssembly);

        services.AddKeyedSingleton<IMessagePolicy>(RegistrationsModule.Key, new RegistrationsMessagePolicy());

        services.AddScoped<ITicketedEventIdLookup, RegistrationsTicketedEventIdLookup>();

        services.AddScoped<IRegistrationsFacade, RegistrationsFacade>();

        services.AddMemoryCache();
        services.AddScoped<IEventSigningKeyProvider, EventSigningKeyProvider>();
        services.AddScoped<RegistrationSigner>();

        // Placeholder until the real email-verification token validator ships. Throws
        // NotImplementedException when a token is actually validated. The handler
        // short-circuits on a null token *before* calling this, so requests without a
        // token still get a clean 400 (email.verification_required).
        services.AddSingleton<IEmailVerificationTokenValidator, NotImplementedEmailVerificationTokenValidator>();

        return services;
    }
}