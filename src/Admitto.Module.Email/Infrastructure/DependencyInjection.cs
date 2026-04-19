using Amolenk.Admitto.Module.Email.Application;
using Amolenk.Admitto.Module.Email.Application.Persistence;
using Amolenk.Admitto.Module.Email.Contracts;
using Amolenk.Admitto.Module.Email.Infrastructure.Persistence;
using Amolenk.Admitto.Module.Email.Infrastructure.Security;
using Amolenk.Admitto.Module.Shared.Infrastructure;
using Amolenk.Admitto.Module.Shared.Infrastructure.Persistence;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Hosting;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

public static class EmailDependencyInjection
{
    extension<TBuilder>(TBuilder builder) where TBuilder : IHostApplicationBuilder
    {
        public IHostApplicationBuilder AddEmailInfrastructureServices()
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

            return builder;
        }
    }
}
