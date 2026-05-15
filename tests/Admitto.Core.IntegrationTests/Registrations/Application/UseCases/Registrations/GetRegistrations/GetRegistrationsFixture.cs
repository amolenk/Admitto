using Amolenk.Admitto.Core.Registrations.Domain.Entities;
using Amolenk.Admitto.Core.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;
using Amolenk.Admitto.Core.Organization.Domain.ValueObjects;

namespace Amolenk.Admitto.Core.IntegrationTests.Registrations.Application.UseCases.Registrations.GetRegistrations;

internal sealed class GetRegistrationsFixture
{
    public const string GeneralName = "General Admission";
    public const string VipName = "VIP Pass";

    public TicketedEventId EventId { get; } = TicketedEventId.New();
    public TicketedEventId OtherEventId { get; } = TicketedEventId.New();
    public TeamId TeamId { get; } = TeamId.New();
    public TicketTypeId GeneralId { get; } = TicketTypeId.New();
    public TicketTypeId VipId { get; } = TicketTypeId.New();

    private bool _seedRegistrations;
    private bool _seedMultiTicketRegistration;
    private bool _seedOtherEventRegistration;

    private GetRegistrationsFixture() { }

    public static GetRegistrationsFixture Empty() => new();

    public static GetRegistrationsFixture WithRegistrations() => new()
    {
        _seedRegistrations = true,
    };

    public static GetRegistrationsFixture WithMultiTicketRegistration() => new()
    {
        _seedMultiTicketRegistration = true,
    };

    public static GetRegistrationsFixture WithRegistrationsAcrossEvents() => new()
    {
        _seedRegistrations = true,
        _seedOtherEventRegistration = true,
    };

    public async ValueTask SetupAsync(IntegrationTestEnvironment environment)
    {
        var ticketedEvent = TicketedEvent.Create(
            CreationRequestId.From(Guid.NewGuid()),
            EventId, TeamId,
            EventName.From("Test Event"),
            AbsoluteUrl.From("https://example.com"),
            AbsoluteUrl.From("https://tickets.example.com"),
            DateTimeOffset.UtcNow.AddDays(30),
            DateTimeOffset.UtcNow.AddDays(31),
            TimeZoneId.From("UTC"));

        var catalog = TicketCatalog.Create(EventId);
        catalog.AddTicketType(GeneralId, TicketTypeName.From(GeneralName), [], 100);
        catalog.AddTicketType(VipId, TicketTypeName.From(VipName), [], 25);

        await environment.RegistrationsDatabase.SeedAsync(db =>
        {
            db.TicketedEvents.Add(ticketedEvent);
            db.TicketCatalogs.Add(catalog);
        });

        if (_seedRegistrations)
        {
            var alice = Registration.Create(
                TeamId,
                EventId,
                EmailAddress.From("alice@example.com"),
                FirstName.From("Alice"),
                LastName.From("Doe"),
                [new TicketTypeSnapshot(GeneralId, TicketTypeName.From(GeneralName), [])]);

            var bob = Registration.Create(
                TeamId,
                EventId,
                EmailAddress.From("bob@example.com"),
                FirstName.From("Bob"),
                LastName.From("Doe"),
                [new TicketTypeSnapshot(GeneralId, TicketTypeName.From(GeneralName), [])]);

            await environment.RegistrationsDatabase.SeedAsync(db =>
            {
                db.Registrations.Add(alice);
                db.Registrations.Add(bob);
            });
        }

        if (_seedMultiTicketRegistration)
        {
            var multi = Registration.Create(
                TeamId,
                EventId,
                EmailAddress.From("carol@example.com"),
                FirstName.From("Carol"),
                LastName.From("Doe"),
                [
                    new TicketTypeSnapshot(GeneralId, TicketTypeName.From(GeneralName), []),
                    new TicketTypeSnapshot(VipId, TicketTypeName.From(VipName), []),
                ]);

            await environment.RegistrationsDatabase.SeedAsync(db => db.Registrations.Add(multi));
        }

        if (_seedOtherEventRegistration)
        {
            var otherTicketedEvent = TicketedEvent.Create(
                CreationRequestId.From(Guid.NewGuid()),
                OtherEventId, TeamId,
                EventName.From("Other Event"),
                AbsoluteUrl.From("https://example.com"),
                AbsoluteUrl.From("https://tickets.example.com"),
                DateTimeOffset.UtcNow.AddDays(30),
                DateTimeOffset.UtcNow.AddDays(31),
                TimeZoneId.From("UTC"));

            var otherGeneralId = TicketTypeId.New();
            var otherCatalog = TicketCatalog.Create(OtherEventId);
            otherCatalog.AddTicketType(otherGeneralId, TicketTypeName.From(GeneralName), [], 100);

            var dave = Registration.Create(
                TeamId,
                OtherEventId,
                EmailAddress.From("dave@example.com"),
                FirstName.From("Dave"),
                LastName.From("Doe"),
                [new TicketTypeSnapshot(otherGeneralId, TicketTypeName.From(GeneralName), [])]);

            await environment.RegistrationsDatabase.SeedAsync(db =>
            {
                db.TicketedEvents.Add(otherTicketedEvent);
                db.TicketCatalogs.Add(otherCatalog);
                db.Registrations.Add(dave);
            });
        }
    }
}
