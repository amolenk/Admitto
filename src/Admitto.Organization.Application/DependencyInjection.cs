using System.Reflection;
using Amolenk.Admitto.Organization.Application.Messaging;
using Amolenk.Admitto.Organization.Contracts;
using Amolenk.Admitto.Shared.Application.Messaging;
using FluentValidation;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Hosting;

namespace Amolenk.Admitto.Organization.Application;

public static class DependencyInjection
{
    public static IHostApplicationBuilder AddOrganizationApplicationServices(
        this IHostApplicationBuilder builder,
        HostCapability capabilities = HostCapability.None)
    {
        var services = builder.Services;
        var executingAssembly = Assembly.GetExecutingAssembly();

        services.AddCommandHandlersFromAssembly(executingAssembly, capabilities);
        services.AddDomainEventHandlersFromAssembly(executingAssembly);
        services.AddQueryHandlersFromAssembly(executingAssembly);
        services.AddValidatorsFromAssembly(executingAssembly);

        services.AddScoped<OrganizationFacade>();
        services.AddScoped<IOrganizationFacade>(sp =>
        {
            // TODO Options?
            if (builder.Configuration["ORGANIZATION__CACHING__ENABLED"] != "true")
                return sp.GetRequiredService<OrganizationFacade>();
            
            var inner = sp.GetRequiredService<OrganizationFacade>();
            var memoryCache = sp.GetRequiredService<IMemoryCache>();
            return new CachingOrganizationFacade(inner, memoryCache);
        });

        services.AddKeyedSingleton<IMessagePolicy>(OrganizationModule.Key, new OrganizationMessagePolicy());

        return builder;
    }
}