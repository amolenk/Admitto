using Amolenk.Admitto.Module.Organization.Tests.Application.Builders;
using Amolenk.Admitto.Module.Organization.Tests.Application.Infrastructure.Hosting;
using Amolenk.Admitto.Module.Organization.Domain.Entities;
using Amolenk.Admitto.Module.Organization.Domain.ValueObjects;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Module.Organization.Tests.Application.UseCases.TicketedEvents.CancelTicketType;

internal sealed class CancelTicketTypeFixture
{
    public Guid TeamId { get; private set; }
    public Guid EventId { get; private set; }
    public uint EventVersion { get; private set; }

    public string TicketTypeSlug { get; } = "general";

    private readonly bool _ticketTypeAlreadyCancelled;

    private CancelTicketTypeFixture(bool ticketTypeAlreadyCancelled = false)
    {
        _ticketTypeAlreadyCancelled = ticketTypeAlreadyCancelled;
    }

    /// <summary>Seeds an active event with one active (not cancelled) ticket type.</summary>
    public static CancelTicketTypeFixture ActiveEventWithTicketType() => new();

    /// <summary>Seeds an active event with one already-cancelled ticket type.</summary>
    public static CancelTicketTypeFixture ActiveEventWithCancelledTicketType() => new(ticketTypeAlreadyCancelled: true);

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
            Slug.From("acme-conf-2026"),
            DisplayName.From("Acme Conf 2026"),
            AbsoluteUrl.From("https://conf.acme.org"),
            AbsoluteUrl.From("https://tickets.acme.org/"),
            new TimeWindow(
                new DateTimeOffset(2026, 6, 1, 9, 0, 0, TimeSpan.Zero),
                new DateTimeOffset(2026, 6, 3, 17, 0, 0, TimeSpan.Zero)));

        ticketedEvent.AddTicketType(
            Slug.From(TicketTypeSlug),
            DisplayName.From("General Admission"),
            timeSlots: [new TimeSlot(Slug.From("all-day"))],
            capacity: Capacity.From(200));

        if (_ticketTypeAlreadyCancelled)
        {
            ticketedEvent.CancelTicketType(Slug.From(TicketTypeSlug));
        }

        await environment.Database.SeedAsync(dbContext =>
        {
            dbContext.TicketedEvents.Add(ticketedEvent);
        });

        EventId = ticketedEvent.Id.Value;
        EventVersion = ticketedEvent.Version;
    }
}
