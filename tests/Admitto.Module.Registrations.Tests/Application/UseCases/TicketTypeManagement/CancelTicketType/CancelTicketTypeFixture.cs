using Amolenk.Admitto.Module.Registrations.Domain.Entities;
using Amolenk.Admitto.Module.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Module.Registrations.Tests.Application.Infrastructure.Hosting;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Module.Registrations.Tests.Application.UseCases.TicketTypeManagement.CancelTicketType;

internal sealed class CancelTicketTypeFixture
{
    private bool _ticketTypeAlreadyCancelled;
    private EventLifecycleStatus _eventStatus = EventLifecycleStatus.Active;

    public TicketedEventId EventId { get; } = TicketedEventId.New();
    public string TicketTypeSlug { get; } = "general-admission";

    private CancelTicketTypeFixture()
    {
    }

    public static CancelTicketTypeFixture ActiveEvent() => new();

    public static CancelTicketTypeFixture AlreadyCancelled() => new()
    {
        _ticketTypeAlreadyCancelled = true
    };

    public static CancelTicketTypeFixture CancelledEvent() => new()
    {
        _eventStatus = EventLifecycleStatus.Cancelled
    };

    public async ValueTask SetupAsync(IntegrationTestEnvironment environment)
    {
        await environment.Database.SeedAsync(dbContext =>
        {
            var catalog = TicketCatalog.Create(EventId);
            catalog.AddTicketType(
                Slug.From(TicketTypeSlug),
                DisplayName.From("General Admission"),
                [],
                100);

            if (_ticketTypeAlreadyCancelled)
            {
                catalog.CancelTicketType(Slug.From(TicketTypeSlug));
            }

            if (_eventStatus == EventLifecycleStatus.Cancelled)
            {
                catalog.MarkEventCancelled();
            }

            dbContext.TicketCatalogs.Add(catalog);
        });
    }
}
