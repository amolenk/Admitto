using Amolenk.Admitto.Core.Email.Application;
using Amolenk.Admitto.Core.Email.Application.Persistence;
using Amolenk.Admitto.Core.Email.Application.Sending;
using Amolenk.Admitto.Core.Email.Application.Sending.Bulk;
using Amolenk.Admitto.Core.Email.Infrastructure.Persistence;
using Amolenk.Admitto.Core.Email.Infrastructure.Security;
using Amolenk.Admitto.Core.Email.Infrastructure.Sending;
using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Amolenk.Admitto.Core.Shared.Infrastructure;
using Amolenk.Admitto.Core.Shared.Infrastructure.Persistence;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Hosting;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

public static class EmailDependencyInjection
{
    extension<TBuilder>(TBuilder builder) where TBuilder : IHostApplicationBuilder
    {
        public IHostApplicationBuilder AddEmailInfrastructureServices(
            HostCapability capabilities = HostCapability.None)
        {
            builder.AddModuleDatabaseServices<IEmailWriteStore, EmailDbContext>(EmailModuleKey.Value);

            builder.Services.AddKeyedScoped<IPostgresExceptionMapping, EmailPostgresExceptionMapping>(
                EmailModuleKey.Value);

            // Shared Data Protection key ring persisted to the email schema so the API and Worker hosts
            // can decrypt secrets written by either side.
            builder.Services
                .AddDataProtection()
                .SetApplicationName("Admitto")
                .PersistKeysToDbContext<EmailDbContext>();

            builder.Services.AddSingleton<IProtectedSecret, ProtectedSecret>();

            if (capabilities.HasFlag(HostCapability.Email))
            {
                builder.Services.AddSingleton<IEmailSender, MailKitEmailSender>();
                builder.Services.AddSingleton<IBulkSmtpSender, MailKitBulkSmtpSender>();
            }

            return builder;
        }
    }
}
