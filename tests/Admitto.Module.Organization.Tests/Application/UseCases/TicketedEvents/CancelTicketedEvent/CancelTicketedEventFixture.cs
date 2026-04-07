using Amolenk.Admitto.Module.Organization.Tests.Application.Builders;
using Amolenk.Admitto.Module.Organization.Tests.Application.Infrastructure.Hosting;
using Amolenk.Admitto.Module.Organization.Domain.Entities;
using Amolenk.Admitto.Module.Organization.Domain.ValueObjects;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Module.Organization.Tests.Application.UseCases.TicketedEvents.CancelTicketedEvent;

internal sealed class CancelTicketedEventFixture
{
    public Guid TeamId { get; private set; }
    public Guid EventId { get; private set; }
    public uint EventVersion { get; private set; }

    private readonly bool _alreadyCancelled;

    private CancelTicketedEventFixture(bool alreadyCancelled = false)
    {
        _alreadyCancelled = alreadyCancelled;
    }

    /// <summary>Seeds an active event.</summary>
    public static CancelTicketedEventFixture ActiveEvent() => new();

    /// <summary>Seeds an already cancelled event.</summary>
    public static CancelTicketedEventFixture AlreadyCancelledEvent() => new(alreadyCancelled: true);

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

        if (_alreadyCancelled) ticketedEvent.Cancel();

        await environment.Database.SeedAsync(dbContext =>
        {
            dbContext.TicketedEvents.Add(ticketedEvent);
        });

        EventId = ticketedEvent.Id.Value;
        EventVersion = ticketedEvent.Version;
    }
}
