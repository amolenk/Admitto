using System.Reflection;
using Amolenk.Admitto.Module.Registrations.Application.Messaging;
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
        services.AddQueryHandlersFromAssembly(executingAssembly);
        services.AddValidatorsFromAssembly(executingAssembly);

        services.AddKeyedSingleton<IMessagePolicy>(RegistrationsModule.Key, new RegistrationsMessagePolicy());
        
        return services;
    }
}