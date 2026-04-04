using Amolenk.Admitto.Module.Organization.Tests.Application.Builders;
using Amolenk.Admitto.Module.Organization.Tests.Application.Infrastructure.Hosting;
using Amolenk.Admitto.Module.Organization.Domain.Entities;
using Amolenk.Admitto.Module.Organization.Domain.ValueObjects;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Module.Organization.Tests.Application.UseCases.TicketedEvents.GetTicketedEvent;

internal sealed class GetTicketedEventFixture
{
    public Guid TeamId { get; private set; }
    public string EventSlug { get; } = "acme-conf-2026";

    private GetTicketedEventFixture()
    {
    }

    /// <summary>Seeds an active event with two ticket types for retrieval.</summary>
    public static GetTicketedEventFixture EventWithTicketTypes() => new();

    public async ValueTask SetupAsync(IntegrationTestEnvironment environment)
    {
        var team = new TeamBuilder().WithSlug("acme").WithName("Acme Events").Build();

        await environment.Database.SeedAsync(dbContext =>
        {
            dbContext.Teams.Add(team);
        });

        TeamId = team.Id.Value;

        var ticketedEvent = TicketedEvent.Create(
            team.Id,
            Slug.From(EventSlug),
            DisplayName.From("Acme Conf 2026"),
            AbsoluteUrl.From("https://conf.acme.org"),
            AbsoluteUrl.From("https://tickets.acme.org/"),
            new TimeWindow(
                new DateTimeOffset(2026, 6, 1, 9, 0, 0, TimeSpan.Zero),
                new DateTimeOffset(2026, 6, 3, 17, 0, 0, TimeSpan.Zero)));

        ticketedEvent.AddTicketType(
            Slug.From("general"),
            DisplayName.From("General Admission"),
            timeSlots: [new TimeSlot(Slug.From("all-day"))],
            capacity: Capacity.From(500));

        ticketedEvent.AddTicketType(
            Slug.From("vip"),
            DisplayName.From("VIP Pass"),
            timeSlots: [new TimeSlot(Slug.From("morning")), new TimeSlot(Slug.From("afternoon"))],
            capacity: Capacity.From(50));

        await environment.Database.SeedAsync(dbContext =>
        {
            dbContext.TicketedEvents.Add(ticketedEvent);
        });
    }
}
