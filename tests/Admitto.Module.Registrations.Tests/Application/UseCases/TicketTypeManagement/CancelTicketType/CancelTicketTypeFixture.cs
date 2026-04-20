using Amolenk.Admitto.Module.Registrations.Domain.Entities;
using Amolenk.Admitto.Module.Registrations.Tests.Application.Infrastructure.Hosting;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Module.Registrations.Tests.Application.UseCases.TicketTypeManagement.CancelTicketType;

internal sealed class CancelTicketTypeFixture
{
    private bool _eventCancelled;
    private bool _ticketTypeAlreadyCancelled;

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

            if (_ticketTypeAlreadyCancelled)
            {
                catalog.CancelTicketType(Slug.From(TicketTypeSlug));
            }

            dbContext.TicketedEventLifecycleGuards.Add(guard);
            dbContext.TicketCatalogs.Add(catalog);
        });
    }
}
