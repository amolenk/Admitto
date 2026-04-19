using Amolenk.Admitto.Module.Email.Domain.Entities;
using Amolenk.Admitto.Module.Email.Domain.Tests.Builders;
using Amolenk.Admitto.Module.Email.Tests.Application.Infrastructure.Hosting;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Module.Email.Tests.Application.UseCases.EventEmailSettings;

internal static class EventEmailSettingsFixture
{
    /// <summary>Seeds an existing <see cref="EventEmailSettings"/> row for the given event.</summary>
    public static async ValueTask<(TicketedEventId EventId, uint Version)> SeedBasicAsync(
        IntegrationTestEnvironment environment,
        TicketedEventId? eventId = null,
        string protectedPassword = "ENCRYPTED:seed-password")
    {
        var id = eventId ?? TicketedEventId.New();

        var settings = new EventEmailSettingsBuilder()
            .ForEvent(id)
            .WithBasicAuth(protectedPassword: protectedPassword)
            .Build();

        await environment.Database.SeedAsync(db => db.EventEmailSettings.Add(settings));

        return (id, settings.Version);
    }
}
