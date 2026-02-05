using Amolenk.Admitto.Registrations.Application;
using Amolenk.Admitto.Registrations.Application.Persistence;
using Amolenk.Admitto.Registrations.Infrastructure.Persistence;
using Amolenk.Admitto.Shared.Infrastructure;
using Microsoft.Extensions.Hosting;

namespace Amolenk.Admitto.Registrations.Infrastructure;

public static class DependencyInjection
{
    public static IHostApplicationBuilder AddRegistrationsInfrastructureServices(this IHostApplicationBuilder builder)
    {
        builder.AddModuleDatabaseServices<IRegistrationsWriteStore, RegistrationsDbContext>(RegistrationsModule.Key);

        return builder;
    }
}