using Amolenk.Admitto.Module.Organization.Tests.Application.Builders;
using Amolenk.Admitto.Module.Organization.Tests.Application.Infrastructure.Hosting;
using Amolenk.Admitto.Module.Organization.Domain.Entities;
using Amolenk.Admitto.Module.Organization.Domain.ValueObjects;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Module.Organization.Tests.Application.UseCases.TicketedEvents.CreateTicketedEvent;

internal sealed class CreateTicketedEventFixture
{
    private bool _seedActiveTeam;
    private bool _seedExistingTicketedEventWithSameTeamAndSlug;
    private bool _seedArchivedTeam;

    public Guid TeamIdValue { get; private set; } = Guid.NewGuid();
    public string TicketedEventSlug { get; } = "dotnet-conf";
    public string ExistingTicketedEventName { get; } = "Dotnet Conf";
    public string ExistingTicketedEventWebsiteUrl { get; } = "https://example.com/events/dotnet-conf";
    public string ExistingTicketedEventBaseUrl { get; } = "https://tickets.example.com";
    public DateTimeOffset ExistingTicketedEventStartsAt { get; } = new(2026, 4, 10, 9, 0, 0, TimeSpan.Zero);
    public DateTimeOffset ExistingTicketedEventEndsAt { get; } = new(2026, 4, 12, 17, 0, 0, TimeSpan.Zero);

    private CreateTicketedEventFixture()
    {
    }

    /// <summary>Seeds an active team so the handler can load it.</summary>
    public static CreateTicketedEventFixture ActiveTeam() => new()
    {
        _seedActiveTeam = true
    };

    /// <summary>Seeds an active team and an existing event with the same slug to trigger the duplicate constraint.</summary>
    public static CreateTicketedEventFixture DuplicateSlugWithinSameTeam() => new()
    {
        _seedActiveTeam = true,
        _seedExistingTicketedEventWithSameTeamAndSlug = true
    };

    /// <summary>Seeds an archived team so event creation is rejected.</summary>
    public static CreateTicketedEventFixture ArchivedTeam() => new()
    {
        _seedArchivedTeam = true
    };

    public async ValueTask SetupAsync(IntegrationTestEnvironment environment)
    {
        if (_seedActiveTeam || _seedExistingTicketedEventWithSameTeamAndSlug)
        {
            var activeTeam = new TeamBuilder()
                .WithSlug("acme")
                .WithName("Acme Events")
                .Build();

            await environment.Database.SeedAsync(dbContext =>
            {
                dbContext.Teams.Add(activeTeam);
            });

            TeamIdValue = activeTeam.Id.Value;
        }

        if (_seedExistingTicketedEventWithSameTeamAndSlug)
        {
            var existingTicketedEvent = TicketedEvent.Create(
                TeamId.From(TeamIdValue),
                Slug.From(TicketedEventSlug),
                DisplayName.From(ExistingTicketedEventName),
                AbsoluteUrl.From(ExistingTicketedEventWebsiteUrl),
                AbsoluteUrl.From(ExistingTicketedEventBaseUrl),
                new TimeWindow(ExistingTicketedEventStartsAt, ExistingTicketedEventEndsAt));

            await environment.Database.SeedAsync(dbContext =>
            {
                dbContext.TicketedEvents.Add(existingTicketedEvent);
            });
        }

        if (_seedArchivedTeam)
        {
            var archivedTeam = new TeamBuilder()
                .WithSlug("acme")
                .WithName("Acme Events")
                .AsArchived()
                .Build();

            await environment.Database.SeedAsync(dbContext =>
            {
                dbContext.Teams.Add(archivedTeam);
            });

            TeamIdValue = archivedTeam.Id.Value;
        }
    }
}
