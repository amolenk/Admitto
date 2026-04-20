using Amolenk.Admitto.Module.Registrations.Domain.Entities;
using Amolenk.Admitto.Module.Registrations.Tests.Application.Infrastructure.Hosting;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Module.Registrations.Tests.Application.UseCases.TicketTypeManagement.UpdateTicketType;

internal sealed class UpdateTicketTypeFixture
{
    private bool _eventCancelled;

    public TicketedEventId EventId { get; } = TicketedEventId.New();
    public string TicketTypeSlug { get; } = "general-admission";

    private UpdateTicketTypeFixture()
    {
    }

    public static UpdateTicketTypeFixture ActiveEvent() => new();

    public static UpdateTicketTypeFixture CancelledEvent() => new()
    {
        _eventCancelled = true
    };

    public async ValueTask SetupAsync(IntegrationTestEnvironment environment)
    {
        await environment.Database.SeedAsync(dbContext =>
        {
            var guard = TicketedEventLifecycleGuard.Create(EventId);
            if (_eventCancelled)
            {
                guard.SetCancelled();
            }

            var catalog = TicketCatalog.Create(EventId);
            catalog.AddTicketType(
                Slug.From(TicketTypeSlug),
                DisplayName.From("General Admission"),
                [],
                100);

            dbContext.TicketedEventLifecycleGuards.Add(guard);
            dbContext.TicketCatalogs.Add(catalog);
        });
    }
}
