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
using Microsoft.Extensions.Hosting;

namespace Amolenk.Admitto.Module.Registrations.Application;

public static class DependencyInjection
{
    public static IHostApplicationBuilder AddRegistrationsApplicationServices(
        this IHostApplicationBuilder builder,
        HostCapability capabilities = HostCapability.None)
    {
        var services = builder.Services;
        var configuration = builder.Configuration;
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

        services.Configure<VerificationTokenOptions>(
            configuration.GetSection(VerificationTokenOptions.SectionName));
        services.AddScoped<IVerificationTokenService, HmacVerificationTokenService>();

        services.Configure<OtpOptions>(
            configuration.GetSection(OtpOptions.SectionName));

        return builder;
    }
}