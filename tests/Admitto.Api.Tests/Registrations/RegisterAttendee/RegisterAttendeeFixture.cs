using Amolenk.Admitto.Api.Tests.Infrastructure.Hosting;
using Amolenk.Admitto.Organization.Application.Tests.Builders;
using Amolenk.Admitto.Registrations.Application.Tests.Builders;
using Amolenk.Admitto.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Api.Tests.Registrations.RegisterAttendee;

internal sealed class RegisterAttendeeFixture
{
    private const string TeamSlug = "default-team";
    private const string EventSlug = "default-event";

    private int _ticketCapacity = 10;

    public Guid TicketTypeId { get; } = Guid.Parse("11111111-1111-1111-1111-111111111111");

    public static string Route => $"/admin/teams/{TeamSlug}/events/{EventSlug}/registrations";

    private RegisterAttendeeFixture() { }

    public static RegisterAttendeeFixture HappyFlow() => new();

    public static RegisterAttendeeFixture SoldOut() =>
        new () { _ticketCapacity = 0 };

    public async ValueTask SetupAsync(EndToEndTestEnvironment environment)
    {
        var team = new TeamBuilder()
            .WithSlug(Organization.Domain.ValueObjects.TeamSlug.From(TeamSlug))
            .Build();

        var ticketType = new TicketTypeRecordBuilder()
            .WithId(TicketTypeId)
            .Build();

        var ticketedEvent = new TicketedEventRecordBuilder()
            .WithSlug(EventSlug)
            .WithTeamId(team.Id.Value)
            .WithTicketType(ticketType)
            .Build();

        var capacityInformation = new TicketedEventCapacityBuilder()
            .WithEventId(new TicketedEventId(ticketedEvent.Id))
            .WithTicketTypeCapacity(TicketTypeId, _ticketCapacity)
            .Build();

        await environment.OrganizationDatabase.SeedAsync(db =>
        {
            db.Teams.Add(team);
            db.TicketedEvents.Add(ticketedEvent);
        });

        await environment.RegistrationsDatabase.SeedAsync(db =>
        {
            db.TicketedEventCapacities.Add(capacityInformation);
        });
    }
}