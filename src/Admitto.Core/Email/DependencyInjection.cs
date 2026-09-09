using System.Reflection;
using Amolenk.Admitto.Core.Email;
using Amolenk.Admitto.Core.Email.Application.Jobs;
using Amolenk.Admitto.Core.Email.Application.Persistence;
using Amolenk.Admitto.Core.Email.Application.Sending;
using Amolenk.Admitto.Core.Email.Application.Sending.Settings;
using Amolenk.Admitto.Core.Email.Application.Composing;
using Amolenk.Admitto.Core.Email.Infrastructure.Persistence;
using Amolenk.Admitto.Core.Email.Infrastructure.Sending;
using Amolenk.Admitto.Core.Shared.Infrastructure.Messaging;
using Amolenk.Admitto.Core.Shared.Infrastructure.Persistence;
using Microsoft.Extensions.Options;
using Quartz;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

public static class EmailModuleExtensions
{
    extension(IHostApplicationBuilder builder)
    {
        public IHostApplicationBuilder AddEmailModule(bool includeWorkerHandlers = false)
        {
            var services = builder.Services;
            var assembly = Assembly.GetExecutingAssembly();

            services.AddQuartz();

            // Command handlers
            services.AddCommandHandlersFromAssembly(assembly, EmailModule.NamespacePrefix, includeWorkerHandlers);

            // Query handlers
            services.AddQueryHandlersFromAssembly(assembly, EmailModule.NamespacePrefix);

            // Domain event handlers
            services.AddDomainEventHandlersFromAssembly(assembly, EmailModule.NamespacePrefix);

            services.AddValidatorsFromAssembly(assembly, EmailModule.NamespacePrefix);

            // Message type registry contribution
            // services.AddSingleton<Action<MessageTypeRegistryBuilder>>(b => b.AddFromAssembly(
            //     assembly,
            //     EmailModule.NamespacePrefix));

            // Infrastructure
            builder.AddModuleDatabaseServices<IEmailWriteStore, EmailDbContext>(EmailModule.Key);

            // Read store for Email-owned projections (same DbContext instance).
            services.AddScoped<IEmailReadStore>(sp => sp.GetRequiredService<EmailDbContext>());

            services.AddKeyedScoped<IPostgresExceptionMapping, EmailPostgresExceptionMapping>(
                EmailModule.Key);

            return builder;
        }

        public IHostApplicationBuilder AddEmailModuleWorker()
        {
            builder.AddEmailModule(includeWorkerHandlers: true);

            var services = builder.Services;
            var assembly = Assembly.GetExecutingAssembly();

            services.AddScoped<ISmtpTransportSettingsResolver, SmtpTransportSettingsResolver>();
            services.AddSingleton<IEmailRenderer, ScribanEmailRenderer>();
            services.AddScoped<ITransactionalEmailComposer, TransactionalEmailComposer>();
            services.Configure<EmailDeliveryOptions>(
                builder.Configuration.GetSection("Email:Delivery"));
            services.Configure<SystemEmailOptions>(
                builder.Configuration.GetSection(SystemEmailOptions.SectionName));
            services.Configure<PublicEventLinksOptions>(
                builder.Configuration.GetSection(PublicEventLinksOptions.SectionName));

            var reconfirmationOptions = ReconfirmationJobOptions.Parse(
                builder.Configuration[ReconfirmationJobOptions.IntervalConfigurationKey]);
            services.AddSingleton<IOptions<ReconfirmationJobOptions>>(
                Microsoft.Extensions.Options.Options.Create(reconfirmationOptions));

            services.AddSingleton<IEmailSender, MailKitEmailSender>();
            services.AddSingleton<ISmtpBatchSender, MailKitSmtpBatchSender>();

            // Integration event handlers
            services.AddIntegrationEventHandlersFromAssembly(assembly, EmailModule.NamespacePrefix);

            // Quartz job registrations (hosted service is started once by AddSharedInfrastructureQueueConsumer)
            services.AddQuartz(options =>
            {
                // One stable trigger evaluates all active projected policies. The interval is
                // captured at Worker startup; changing it requires a Worker restart.
                options.AddJob<SendReconfirmationEmailsJob>(c => c
                    .StoreDurably()
                    .WithIdentity(SendReconfirmationEmailsJob.Name));

                options.AddTrigger(trigger => trigger
                    .ForJob(SendReconfirmationEmailsJob.Name)
                    .WithIdentity(SendReconfirmationEmailsJob.TriggerName)
                    .WithSimpleSchedule(schedule => schedule
                        .WithInterval(reconfirmationOptions.Interval)
                        .RepeatForever())
                    .StartNow());

            });
            services.Configure<QuartzOptions>(options =>
            {
                options.Scheduling.OverWriteExistingData = true;
                options.Scheduling.IgnoreDuplicates = false;
            });

            return builder;
        }
    }
}
