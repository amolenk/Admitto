using System.Reflection;
using Amolenk.Admitto.Application.Common.Cryptography;
using Amolenk.Admitto.Application.Common.Email;
using Amolenk.Admitto.Application.Common.Email.Composing;
using Amolenk.Admitto.Application.Common.Email.Templating;
using Amolenk.Admitto.Application.Common.Messaging;
using Amolenk.Admitto.Application.Common.Persistence;
using Amolenk.Admitto.Application.Jobs.SendCustomBulkEmail;
using FluentValidation.Internal;
using Humanizer;
using Microsoft.Extensions.Caching.Memory;
using Quartz;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

public static class DependencyInjection
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddApplicationApplicationEventHandlers()
        {
            services.Scan(scan => scan
                .FromAssemblies(Assembly.GetExecutingAssembly())
                .AddClasses(classes => classes.AssignableTo<IApplicationEventHandler>())
                .AsSelf()
                .As(t => t.GetInterfaces()
                    .Where(i => i.IsGenericType &&
                                i.GetGenericTypeDefinition() == typeof(IApplicationEventHandler<>)))
                .WithScopedLifetime());
            
            return services;
        }

        public IServiceCollection AddApplicationAuthorizationServices()
        {
            services
                .AddScoped<IAdministratorRoleService, AdministratorRoleService>()
                .AddScoped<ITeamMemberRoleService, CachingTeamMemberRoleService>(sp =>
                {
                    var innerService = new TeamMemberRoleService(sp.GetRequiredService<IApplicationContext>());
                    var cache = sp.GetRequiredService<IMemoryCache>();
                    var logger = sp.GetRequiredService<ILogger<CachingTeamMemberRoleService>>();
                    return new CachingTeamMemberRoleService(innerService, cache, logger);
                });

            return services;
        }

        public IServiceCollection AddApplicationApiCommandHandlers()
        {
            services.Scan(scan => scan
                .FromAssemblies(Assembly.GetExecutingAssembly())
                .AddClasses(classes => classes.AssignableTo<IApiCommandHandler>())
                .AsSelf()
                .As(t => t.GetInterfaces()
                    .Where(i => i.IsGenericType &&
                                i.GetGenericTypeDefinition() == typeof(IApiCommandHandler<>)))
                .WithScopedLifetime());

            return services;
        }
        
        public IServiceCollection AddApplicationCryptographyServices()
        {
            services.AddScoped<ISigningService, SigningService>();
            
            return services;
        }
        
        public IServiceCollection AddApplicationEmailServices()
        {
            services
                .AddKeyedScoped<IEmailComposer, CanceledEmailComposer>(WellKnownEmailType.Canceled)
                .AddKeyedScoped<IEmailComposer, ReconfirmEmailComposer>(WellKnownEmailType.Reconfirm)
                .AddKeyedScoped<IEmailComposer, TicketEmailComposer>(WellKnownEmailType.Ticket)
                .AddKeyedScoped<IEmailComposer, VerificationEmailComposer>(WellKnownEmailType.VerifyEmail)
                .AddKeyedScoped<IEmailComposer, VisaLetterDeniedEmailComposer>(WellKnownEmailType.VisaLetterDenied);

            services
                .AddScoped<IEmailComposerRegistry, EmailComposerRegistry>()
                .AddScoped<IEmailTemplateService, EmailTemplateService>()
                .AddScoped<IEmailDispatcher, EmailDispatcher>()
                .AddScoped<CustomEmailComposer>();

            return services;
        }
        
        public IServiceCollection AddApplicationEventualDomainEventHandlers()
        {
            services.Scan(scan => scan
                .FromAssemblies(Assembly.GetExecutingAssembly())
                .AddClasses(classes => classes.AssignableTo<IEventualDomainEventHandler>())
                .AsSelf()
                .As(t => t.GetInterfaces()
                    .Where(i => i.IsGenericType &&
                                i.GetGenericTypeDefinition() == typeof(IEventualDomainEventHandler<>)))
                .WithScopedLifetime());
            
            return services;
        }

        public IServiceCollection AddApplicationJobs()
        {
            services.AddQuartz(options =>
            {
                options.AddJob<SendCustomBulkEmailJob>(c => c
                    .StoreDurably()
                    .WithIdentity(SendCustomBulkEmailJob.Name));
            });

            return services;
        }
        
        public IServiceCollection AddApplicationTransactionalDomainEventHandlers()
        {
            services.Scan(scan => scan
                .FromAssemblies(Assembly.GetExecutingAssembly())
                .AddClasses(classes => classes.AssignableTo<ITransactionalDomainEventHandler>())
                .AsSelf()
                .As(t => t.GetInterfaces()
                    .Where(i => i.IsGenericType &&
                                i.GetGenericTypeDefinition() == typeof(ITransactionalDomainEventHandler<>)))
                .WithScopedLifetime());
            
            return services;
        }
        
        public IServiceCollection AddApplicationValidationServices()
        {
            services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

            // Use camel case for FluentValidation property names
            ValidatorOptions.Global.DisplayNameResolver = (_, member, _) => member?.Name.Humanize();
            ValidatorOptions.Global.PropertyNameResolver = (_, memberInfo, expression) =>
            {
                if (expression != null)
                {
                    var chain = PropertyChain.FromExpression(expression);
                    if (chain.Count > 0)
                    {
                        var propertyNames = chain.ToString().Split(ValidatorOptions.Global.PropertyChainSeparator);
                        if (propertyNames.Length == 1)
                        {
                            return propertyNames[0].Camelize();
                        }

                        return string.Join(
                            ValidatorOptions.Global.PropertyChainSeparator,
                            propertyNames.Select(n => n.Camelize()));
                    }
                }

                return memberInfo?.Name.Camelize();
            };

            return services;
        }
        
        public IServiceCollection AddApplicationWorkerCommandHandlers()
        {
            services.Scan(scan => scan
                .FromAssemblies(Assembly.GetExecutingAssembly())
                .AddClasses(classes => classes.AssignableTo<IWorkerCommandHandler>())
                .AsSelf()
                .As(t => t.GetInterfaces()
                    .Where(i => i.IsGenericType &&
                                i.GetGenericTypeDefinition() == typeof(IWorkerCommandHandler<>)))
                .WithScopedLifetime());

            return services;
        }
    }
}