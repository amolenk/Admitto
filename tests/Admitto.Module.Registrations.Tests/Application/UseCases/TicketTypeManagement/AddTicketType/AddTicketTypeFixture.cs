using Amolenk.Admitto.Module.Registrations.Domain.Entities;
using Amolenk.Admitto.Module.Registrations.Tests.Application.Infrastructure.Hosting;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Module.Registrations.Tests.Application.UseCases.TicketTypeManagement.AddTicketType;

internal sealed class AddTicketTypeFixture
{
    private bool _eventCancelled;
    private bool _seedPolicy = true;
    private bool _seedCatalog;

    public TicketedEventId EventId { get; } = TicketedEventId.New();

    private AddTicketTypeFixture()
    {
    }

    public static AddTicketTypeFixture ActiveEvent() => new();

    public static AddTicketTypeFixture ActiveEventWithCatalog() => new()
    {
        _seedCatalog = true
    };

    public static AddTicketTypeFixture CancelledEvent() => new()
    {
        _eventCancelled = true
    };

    public static AddTicketTypeFixture NoPolicyExists() => new()
    {
        _seedPolicy = false
    };

    public async ValueTask SetupAsync(IntegrationTestEnvironment environment)
    {
        await environment.Database.SeedAsync(dbContext =>
        {
            if (_seedPolicy)
            {
                var policy = EventRegistrationPolicy.Create(EventId);
                if (_eventCancelled)
                {
                    policy.SetCancelled();
                }
                dbContext.EventRegistrationPolicies.Add(policy);
            }

            if (_seedCatalog)
            {
                var catalog = TicketCatalog.Create(EventId);
                catalog.AddTicketType(
                    Slug.From("existing-type"),
                    DisplayName.From("Existing Type"),
                    [],
                    100);
                dbContext.TicketCatalogs.Add(catalog);
            }
        });
    }
}
