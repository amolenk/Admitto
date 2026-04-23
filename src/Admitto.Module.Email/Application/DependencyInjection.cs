using System.Reflection;
using Amolenk.Admitto.Module.Email.Application.Messaging;
using Amolenk.Admitto.Module.Email.Application.Settings;
using Amolenk.Admitto.Module.Email.Application.Templating;
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
        services.AddIntegrationEventHandlersFromAssembly(executingAssembly, EmailModuleKey.Value);
        services.AddQueryHandlersFromAssembly(executingAssembly);
        services.AddValidatorsFromAssembly(executingAssembly);

        services.AddKeyedSingleton<IMessagePolicy, EmailMessagePolicy>(EmailModuleKey.Value);

        services.AddScoped<IEffectiveEmailSettingsResolver, EffectiveEmailSettingsResolver>();
        services.AddScoped<IEmailTemplateService, EmailTemplateService>();
        services.AddSingleton<IEmailRenderer, ScribanEmailRenderer>();

        return builder;
    }
}
