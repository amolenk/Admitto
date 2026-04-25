using Amolenk.Admitto.Module.Registrations.Domain.Entities;
using Amolenk.Admitto.Module.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Module.Registrations.Tests.Application.Infrastructure.Hosting;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Module.Registrations.Tests.Application.UseCases.Registrations.GetRegistrations;

internal sealed class GetRegistrationsFixture
{
    public const string GeneralSlug = "general-admission";
    public const string GeneralName = "General Admission";
    public const string VipSlug = "vip-pass";
    public const string VipName = "VIP Pass";

    public TicketedEventId EventId { get; } = TicketedEventId.New();
    public TicketedEventId OtherEventId { get; } = TicketedEventId.New();
    public TeamId TeamId { get; } = TeamId.New();

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
        var catalog = TicketCatalog.Create(EventId);
        catalog.AddTicketType(Slug.From(GeneralSlug), DisplayName.From(GeneralName), [], 100);
        catalog.AddTicketType(Slug.From(VipSlug), DisplayName.From(VipName), [], 25);

        await environment.Database.SeedAsync(db => db.TicketCatalogs.Add(catalog));

        if (_seedRegistrations)
        {
            var alice = Registration.Create(
                TeamId,
                EventId,
                EmailAddress.From("alice@example.com"),
                FirstName.From("Alice"),
                LastName.From("Doe"),
                [new TicketTypeSnapshot(GeneralSlug, [])]);

            var bob = Registration.Create(
                TeamId,
                EventId,
                EmailAddress.From("bob@example.com"),
                FirstName.From("Bob"),
                LastName.From("Doe"),
                [new TicketTypeSnapshot(GeneralSlug, [])]);

            await environment.Database.SeedAsync(db =>
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
                    new TicketTypeSnapshot(GeneralSlug, []),
                    new TicketTypeSnapshot(VipSlug, []),
                ]);

            await environment.Database.SeedAsync(db => db.Registrations.Add(multi));
        }

        if (_seedOtherEventRegistration)
        {
            var otherCatalog = TicketCatalog.Create(OtherEventId);
            otherCatalog.AddTicketType(Slug.From(GeneralSlug), DisplayName.From(GeneralName), [], 100);

            var dave = Registration.Create(
                TeamId,
                OtherEventId,
                EmailAddress.From("dave@example.com"),
                FirstName.From("Dave"),
                LastName.From("Doe"),
                [new TicketTypeSnapshot(GeneralSlug, [])]);

            await environment.Database.SeedAsync(db =>
            {
                db.TicketCatalogs.Add(otherCatalog);
                db.Registrations.Add(dave);
            });
        }
    }
}
