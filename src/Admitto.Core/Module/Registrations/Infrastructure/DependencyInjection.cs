using Amolenk.Admitto.Core.Module.Registrations.Application;
using Amolenk.Admitto.Core.Module.Registrations.Application.Persistence;
using Amolenk.Admitto.Core.Module.Registrations.Infrastructure.Persistence;
using Amolenk.Admitto.Core.Shared.Infrastructure;
using Amolenk.Admitto.Core.Shared.Infrastructure.Persistence;
using Microsoft.Extensions.Hosting;

namespace Amolenk.Admitto.Core.Module.Registrations.Infrastructure;

public static class DependencyInjection
{
    public static IHostApplicationBuilder AddRegistrationsInfrastructureServices(this IHostApplicationBuilder builder)
    {
        builder.AddModuleDatabaseServices<IRegistrationsWriteStore, RegistrationsDbContext>(RegistrationsModule.Key);

        builder.Services.AddKeyedScoped<IPostgresExceptionMapping, RegistrationsPostgresExceptionMapping>(
            RegistrationsModule.Key);

        return builder;
    }
}