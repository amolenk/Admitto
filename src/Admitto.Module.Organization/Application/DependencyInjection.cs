using System.Reflection;
using Amolenk.Admitto.Module.Organization.Application.Mapping;
using Amolenk.Admitto.Module.Organization.Application.Messaging;
using Amolenk.Admitto.Module.Organization.Application.UseCases;
using Amolenk.Admitto.Module.Organization.Contracts;
using Amolenk.Admitto.Module.Shared.Application.Messaging;
using Amolenk.Admitto.Module.Shared.Infrastructure.Persistence;
using FluentValidation;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Hosting;

namespace Amolenk.Admitto.Module.Organization.Application;

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
        services.AddModuleEventHandlersFromAssembly(executingAssembly);
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

        services.AddKeyedSingleton<IMessagePolicy, OrganizationMessagePolicy>(
            OrganizationModuleKey.Value);

        return builder;
    }
}
