using Amolenk.Admitto.Module.Organization.Tests.Application.Builders;
using Amolenk.Admitto.Module.Organization.Tests.Application.Infrastructure.Hosting;
using Amolenk.Admitto.Module.Organization.Domain.Entities;
using Amolenk.Admitto.Module.Organization.Domain.ValueObjects;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Module.Organization.Tests.Application.UseCases.TicketedEvents.AddTicketType;

internal sealed class AddTicketTypeFixture
{
    public Guid TeamId { get; private set; }
    public Guid EventId { get; private set; }
    public uint EventVersion { get; private set; }

    public string ExistingTicketTypeSlug { get; } = "general";

    private readonly bool _cancelled;
    private readonly bool _withExistingTicketType;

    private AddTicketTypeFixture(bool cancelled = false, bool withExistingTicketType = false)
    {
        _cancelled = cancelled;
        _withExistingTicketType = withExistingTicketType;
    }

    /// <summary>Seeds an active event with no ticket types.</summary>
    public static AddTicketTypeFixture ActiveEventWithNoTicketTypes() => new();

    /// <summary>Seeds an active event that already has a ticket type with the "general" slug.</summary>
    public static AddTicketTypeFixture ActiveEventWithExistingTicketType() => new(withExistingTicketType: true);

    /// <summary>Seeds a cancelled event.</summary>
    public static AddTicketTypeFixture CancelledEvent() => new(cancelled: true);

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

        if (_withExistingTicketType)
        {
            ticketedEvent.AddTicketType(
                Slug.From(ExistingTicketTypeSlug),
                DisplayName.From("General Admission"),
                isSelfService: true,
                isSelfServiceAvailable: true,
                timeSlots: [new TimeSlot(Slug.From("all-day"))],
                capacity: Capacity.From(200));
        }

        if (_cancelled) ticketedEvent.Cancel();

        await environment.Database.SeedAsync(dbContext =>
        {
            dbContext.TicketedEvents.Add(ticketedEvent);
        });

        EventId = ticketedEvent.Id.Value;
        EventVersion = ticketedEvent.Version;
    }
}
