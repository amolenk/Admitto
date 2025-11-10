using System.Reflection;
using Amolenk.Admitto.Application;
using Amolenk.Admitto.Application.Common.Core;
using Amolenk.Admitto.Application.Common.Email;
using Amolenk.Admitto.Application.Common.Email.Composing;
using Amolenk.Admitto.Application.Common.Email.Templating;
using Amolenk.Admitto.Application.Jobs;
using Amolenk.Admitto.Application.Jobs.SendBulkEmail;
using Amolenk.Admitto.Domain.ValueObjects;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

public static class DependencyInjection
{
    // TODO Split up further
    public static void AddDefaultApplicationServices(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

        AddTransactionalDomainEventHandlers(services);
    }
    
    public static void AddCommandHandlers(this IServiceCollection services)
    {
        services.Scan(scan => scan
            .FromAssemblyOf<ApplicationAssemblyLocator>()
            .AddClasses(classes => classes.AssignableTo<ICommandHandler>())
            .AsSelf()
            .As(t => t.GetInterfaces()
                .Where(i => i.IsGenericType &&
                            i.GetGenericTypeDefinition() == typeof(ICommandHandler<>)))
            .WithScopedLifetime());
    }

    public static void AddJobHandlers(this IServiceCollection services)
    {
        services.AddKeyedScoped<IJobHandler, SendBulkEmailJobHandler>(WellKnownJob.SendBulkEmails);
    }

    public static void AddTransactionalDomainEventHandlers(this IServiceCollection services)
    {
        services.Scan(scan => scan
            .FromAssemblyOf<ApplicationAssemblyLocator>()
            .AddClasses(classes => classes.AssignableTo<ITransactionalDomainEventHandler>())
            .AsSelf()
            .As(t => t.GetInterfaces()
                .Where(i => i.IsGenericType &&
                            i.GetGenericTypeDefinition() == typeof(ITransactionalDomainEventHandler<>)))
            .WithScopedLifetime());
    }

    public static void AddEventualDomainEventHandlers(this IServiceCollection services)
    {
        services.Scan(scan => scan
            .FromAssemblyOf<ApplicationAssemblyLocator>()
            .AddClasses(classes => classes.AssignableTo<IEventualDomainEventHandler>())
            .AsSelf()
            .As(t => t.GetInterfaces()
                .Where(i => i.IsGenericType &&
                            i.GetGenericTypeDefinition() == typeof(IEventualDomainEventHandler<>)))
            .WithScopedLifetime());
    }

    public static void AddApplicationEventHandlers(this IServiceCollection services)
    {
        services.Scan(scan => scan
            .FromAssemblyOf<ApplicationAssemblyLocator>()
            .AddClasses(classes => classes.AssignableTo<IApplicationEventHandler>())
            .AsSelf()
            .As(t => t.GetInterfaces()
                .Where(i => i.IsGenericType &&
                    i.GetGenericTypeDefinition() == typeof(IApplicationEventHandler<>)))
            .WithScopedLifetime());
    }

    public static void AddEmailServices(this IServiceCollection services)
    {
        services.AddKeyedScoped<IEmailComposer, CanceledEmailComposer>(WellKnownEmailType.Canceled);
        services.AddKeyedScoped<IEmailComposer, VisaLetterDeniedEmailComposer>(WellKnownEmailType.VisaLetterDenied);
        services.AddKeyedScoped<IEmailComposer, ReconfirmEmailComposer>(WellKnownEmailType.Reconfirm);
        services.AddKeyedScoped<IEmailComposer, TicketEmailComposer>(WellKnownEmailType.Ticket);
        services.AddKeyedScoped<IEmailComposer, VerificationEmailComposer>(WellKnownEmailType.VerifyEmail);
        
        services.AddScoped<IEmailComposerRegistry, EmailComposerRegistry>();
        services.AddScoped<IEmailTemplateService, EmailTemplateService>();

        services.AddScoped<IEmailDispatcher, EmailDispatcher>();
        services.AddScoped<CustomEmailComposer>();
    }
}