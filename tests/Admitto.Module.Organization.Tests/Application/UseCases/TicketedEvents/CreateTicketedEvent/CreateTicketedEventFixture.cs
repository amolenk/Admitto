using Amolenk.Admitto.Module.Organization.Tests.Application.Infrastructure.Hosting;
using Amolenk.Admitto.Module.Organization.Domain.Entities;
using Amolenk.Admitto.Module.Organization.Domain.ValueObjects;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Module.Organization.Tests.Application.UseCases.TicketedEvents.CreateTicketedEvent;

internal sealed class CreateTicketedEventFixture
{
    private bool _seedExistingTicketedEventWithSameTeamAndSlug;

    public Guid TeamIdValue { get; } = Guid.NewGuid();
    public string TicketedEventSlug { get; } = "dotnet-conf";
    public string ExistingTicketedEventName { get; } = "Dotnet Conf";
    public string ExistingTicketedEventWebsiteUrl { get; } = "https://example.com/events/dotnet-conf";
    public string ExistingTicketedEventBaseUrl { get; } = "https://tickets.example.com";
    public DateTimeOffset ExistingTicketedEventStartsAt { get; } = new(2026, 4, 10, 9, 0, 0, TimeSpan.Zero);
    public DateTimeOffset ExistingTicketedEventEndsAt { get; } = new(2026, 4, 12, 17, 0, 0, TimeSpan.Zero);

    private CreateTicketedEventFixture()
    {
    }

    public static CreateTicketedEventFixture DuplicateSlugWithinSameTeam() => new()
    {
        _seedExistingTicketedEventWithSameTeamAndSlug = true
    };

    public async ValueTask SetupAsync(IntegrationTestEnvironment environment)
    {
        if (!_seedExistingTicketedEventWithSameTeamAndSlug)
        {
            return;
        }

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
}
