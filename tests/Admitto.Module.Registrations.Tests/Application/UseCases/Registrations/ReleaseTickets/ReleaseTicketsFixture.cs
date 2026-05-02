using Amolenk.Admitto.Module.Registrations.Domain.Entities;
using Amolenk.Admitto.Module.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Module.Registrations.Tests.Application.Infrastructure.Hosting;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Module.Registrations.Tests.Application.UseCases.Registrations.ReleaseTickets;

internal sealed class ReleaseTicketsFixture
{
    private TicketCatalog? _catalog;

    public TicketedEventId EventId { get; } = TicketedEventId.New();
    public TeamId TeamId { get; } = TeamId.New();
    public RegistrationId RegistrationId { get; private set; } = RegistrationId.New();
    public string TicketTypeSlug { get; private set; } = "general-admission";

    private ReleaseTicketsFixture() { }

    public static ReleaseTicketsFixture WithCatalogAndRegistration(
        int maxCapacity = 10,
        int usedCapacity = 3,
        string ticketTypeSlug = "general-admission")
    {
        var f = new ReleaseTicketsFixture { TicketTypeSlug = ticketTypeSlug };
        var catalog = TicketCatalog.Create(f.EventId);
        catalog.AddTicketType(Slug.From(ticketTypeSlug), DisplayName.From("General Admission"), [], maxCapacity);
        for (var i = 0; i < usedCapacity; i++)
            catalog.Claim([ticketTypeSlug], enforce: false);
        f._catalog = catalog;
        return f;
    }

    public static ReleaseTicketsFixture WithoutCatalog() => new();

    public static ReleaseTicketsFixture WithCatalogAtZeroCapacity(string ticketTypeSlug = "general-admission")
    {
        var f = new ReleaseTicketsFixture { TicketTypeSlug = ticketTypeSlug };
        var catalog = TicketCatalog.Create(f.EventId);
        catalog.AddTicketType(Slug.From(ticketTypeSlug), DisplayName.From("General Admission"), [], 10);
        f._catalog = catalog;
        return f;
    }

    public static ReleaseTicketsFixture WithCatalogAndUnknownSlugInRegistration()
    {
        var f = new ReleaseTicketsFixture { TicketTypeSlug = "ghost-ticket" };
        var catalog = TicketCatalog.Create(f.EventId);
        catalog.AddTicketType(Slug.From("known-ticket"), DisplayName.From("Known Ticket"), [], 10);
        catalog.Claim(["known-ticket"], enforce: false);
        f._catalog = catalog;
        return f;
    }

    public async ValueTask SetupAsync(IntegrationTestEnvironment environment)
    {
        await environment.Database.SeedAsync(dbContext =>
        {
            if (_catalog is not null)
                dbContext.TicketCatalogs.Add(_catalog);

            var registration = Registration.Create(
                TeamId,
                EventId,
                EmailAddress.From("alice@example.com"),
                FirstName.From("Alice"),
                LastName.From("Test"),
                [new TicketTypeSnapshot(TicketTypeSlug, TicketTypeSlug, [])]);

            RegistrationId = registration.Id;
            dbContext.Registrations.Add(registration);
        });
    }
}
