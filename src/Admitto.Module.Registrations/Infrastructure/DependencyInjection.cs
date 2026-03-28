using Amolenk.Admitto.Module.Registrations.Application;
using Amolenk.Admitto.Module.Registrations.Application.Persistence;
using Amolenk.Admitto.Module.Registrations.Infrastructure.Persistence;
using Amolenk.Admitto.Module.Shared.Infrastructure;
using Microsoft.Extensions.Hosting;

namespace Amolenk.Admitto.Module.Registrations.Infrastructure;

public static class DependencyInjection
{
    public static IHostApplicationBuilder AddRegistrationsInfrastructureServices(this IHostApplicationBuilder builder)
    {
        builder.AddModuleDatabaseServices<IRegistrationsWriteStore, RegistrationsDbContext>(RegistrationsModule.Key);

        return builder;
    }
}