using Amolenk.Admitto.Core.Registrations.Domain.Entities;
using Amolenk.Admitto.Core.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;
using Amolenk.Admitto.Core.Registrations.Contracts.ValueObjects;

namespace Amolenk.Admitto.Core.IntegrationTests.Registrations.Application.UseCases.Registrations.ReleaseTickets;

internal sealed class ReleaseTicketsFixture
{
    private TicketCatalog? _catalog;

    // The ticket type ID that the registration holds.
    // For UnknownTicketType scenario this is a ghost ID (not in catalog).
    private TicketTypeId _registrationTicketTypeId = TicketTypeId.New();

    public TicketedEventId EventId { get; } = TicketedEventId.New();
    public TeamId TeamId { get; } = TeamId.New();
    public RegistrationId RegistrationId { get; private set; } = RegistrationId.New();

    // The ticket type that IS in the catalog (may equal _registrationTicketTypeId or not).
    public TicketTypeId TicketTypeId { get; private set; } = TicketTypeId.New();

    // Only set in WithCatalogAndUnknownTicketTypeInRegistration: the catalog's known ticket type.
    public TicketTypeId KnownTicketTypeId { get; private set; }

    private ReleaseTicketsFixture() { }

    public static ReleaseTicketsFixture WithCatalogAndRegistration(
        int maxCapacity = 10,
        int usedCapacity = 3)
    {
        var f = new ReleaseTicketsFixture();
        f._registrationTicketTypeId = f.TicketTypeId;
        var catalog = TicketCatalog.Create(f.EventId, f.TeamId);
        catalog.AddTicketType(f.TicketTypeId, TicketTypeName.From("General Admission"), [], maxCapacity);
        for (var i = 0; i < usedCapacity; i++)
            catalog.Claim([f.TicketTypeId], enforce: false);
        f._catalog = catalog;
        return f;
    }

    public static ReleaseTicketsFixture WithoutCatalog() => new();

    public static ReleaseTicketsFixture WithCatalogAtZeroCapacity()
    {
        var f = new ReleaseTicketsFixture();
        f._registrationTicketTypeId = f.TicketTypeId;
        var catalog = TicketCatalog.Create(f.EventId, f.TeamId);
        catalog.AddTicketType(f.TicketTypeId, TicketTypeName.From("General Admission"), [], 10);
        f._catalog = catalog;
        return f;
    }

    public static ReleaseTicketsFixture WithCatalogAndUnknownTicketTypeInRegistration()
    {
        var ghostId = TicketTypeId.New();
        var knownId = TicketTypeId.New();
        var f = new ReleaseTicketsFixture
        {
            KnownTicketTypeId = knownId,
        };
        // The registration will hold ghostId (unknown in catalog)
        f._registrationTicketTypeId = ghostId;
        // TicketTypeId points to the catalog's known ticket type for assertions
        f.TicketTypeId = knownId;

        var catalog = TicketCatalog.Create(f.EventId, f.TeamId);
        catalog.AddTicketType(knownId, TicketTypeName.From("Known Ticket"), [], 10);
        catalog.Claim([knownId], enforce: false);
        f._catalog = catalog;
        return f;
    }

    public async ValueTask SetupAsync(IntegrationTestEnvironment environment)
    {
        await environment.RegistrationsDatabase.SeedAsync(dbContext =>
        {
            if (_catalog is not null)
                dbContext.TicketCatalogs.Add(_catalog);

            var registration = Registration.Create(
                TeamId,
                EventId,
                EmailAddress.From("alice@example.com"),
                FirstName.From("Alice"),
                LastName.From("Test"),
                [new TicketTypeSnapshot(_registrationTicketTypeId, TicketTypeName.From("General Admission"), [])]);

            RegistrationId = registration.Id;
            dbContext.Registrations.Add(registration);
        });
    }
}
