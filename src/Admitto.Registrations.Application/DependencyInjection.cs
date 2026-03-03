// using System.Reflection;
// using Amolenk.Admitto.Registrations.Application.Messaging;
// using Amolenk.Admitto.Registrations.Application.Services;
// using Amolenk.Admitto.Registrations.Contracts;
// using Amolenk.Admitto.Shared.Application.Messaging;
// using FluentValidation;
//
// namespace Amolenk.Admitto.Registrations.Application;
//
// public static class DependencyInjection
// {
//     public static IServiceCollection AddRegistrationsApplicationServices(
//         this IServiceCollection services,
//         HostCapability capabilities = HostCapability.None)
//     {
//         var executingAssembly = Assembly.GetExecutingAssembly();
//
//         services.AddCommandHandlersFromAssembly(executingAssembly, capabilities);
//         services.AddDomainEventHandlersFromAssembly(executingAssembly);
//         services.AddQueryHandlersFromAssembly(executingAssembly);
//         services.AddValidatorsFromAssembly(executingAssembly);
//
//         services.AddKeyedSingleton<IMessagePolicy>(RegistrationsModule.Key, new RegistrationsMessagePolicy());
//
//         services.AddScoped<ICapacityTracker, CapacityTracker>();
//         
//         return services;
//     }
// }