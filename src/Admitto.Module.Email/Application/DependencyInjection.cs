using System.Reflection;
using Amolenk.Admitto.Module.Email.Application.Messaging;
using Amolenk.Admitto.Module.Email.Application.UseCases;
using Amolenk.Admitto.Module.Email.Contracts;
using Amolenk.Admitto.Module.Shared.Application.Messaging;
using FluentValidation;
using Microsoft.Extensions.Hosting;

namespace Amolenk.Admitto.Module.Email.Application;

public static class DependencyInjection
{
    public static IHostApplicationBuilder AddEmailApplicationServices(
        this IHostApplicationBuilder builder,
        HostCapability capabilities = HostCapability.None)
    {
        var services = builder.Services;
        var executingAssembly = Assembly.GetExecutingAssembly();

        services.AddCommandHandlersFromAssembly(executingAssembly, capabilities);
        services.AddDomainEventHandlersFromAssembly(executingAssembly);
        services.AddModuleEventHandlersFromAssembly(executingAssembly);
        services.AddQueryHandlersFromAssembly(executingAssembly);
        services.AddValidatorsFromAssembly(executingAssembly);

        services.AddKeyedSingleton<IMessagePolicy, EmailMessagePolicy>(EmailModuleKey.Value);

        // Facade is metadata-only: registered in every host that loads the module, with no
        // capability gate (per email-settings spec: configuration-status facade is not
        // capability-gated).
        services.AddScoped<IEventEmailFacade, EventEmailFacade>();

        return builder;
    }
}
