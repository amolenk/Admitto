using Amolenk.Admitto.Api.Tests.Infrastructure.Hosting;
using Amolenk.Admitto.Module.Organization.Tests.Application.Builders;
using Amolenk.Admitto.Module.Registrations.Domain.Entities;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Api.Tests.Registrations.GetTicketedEvents;

internal sealed class GetTicketedEventsFixture
{
    public const string TeamSlug = "acme";

    public static string Route => $"/admin/teams/{TeamSlug}/events";

    private GetTicketedEventsFixture() { }

    public static GetTicketedEventsFixture WithMixedStatuses() => new();

    public async ValueTask SetupAsync(EndToEndTestEnvironment environment)
    {
        var team = new TeamBuilder()
            .WithSlug(TeamSlug)
            .Build();

        var active = TicketedEvent.Create(
            TicketedEventId.New(),
            team.Id,
            Slug.From(TeamSlug),
            Slug.From("conf-2026"),
            DisplayName.From("Conf 2026"),
            AbsoluteUrl.From("https://example.com"),
            AbsoluteUrl.From("https://tickets.example.com"),
            DateTimeOffset.UtcNow.AddDays(30),
            DateTimeOffset.UtcNow.AddDays(32),
            TimeZoneId.From("UTC"));

        var cancelled = TicketedEvent.Create(
            TicketedEventId.New(),
            team.Id,
            Slug.From(TeamSlug),
            Slug.From("meetup-q1"),
            DisplayName.From("Meetup Q1"),
            AbsoluteUrl.From("https://example.com"),
            AbsoluteUrl.From("https://tickets.example.com"),
            DateTimeOffset.UtcNow.AddDays(10),
            DateTimeOffset.UtcNow.AddDays(11),
            TimeZoneId.From("UTC"));
        cancelled.Cancel();

        var archived = TicketedEvent.Create(
            TicketedEventId.New(),
            team.Id,
            Slug.From(TeamSlug),
            Slug.From("conf-2025"),
            DisplayName.From("Conf 2025"),
            AbsoluteUrl.From("https://example.com"),
            AbsoluteUrl.From("https://tickets.example.com"),
            DateTimeOffset.UtcNow.AddDays(-60),
            DateTimeOffset.UtcNow.AddDays(-58),
            TimeZoneId.From("UTC"));
        archived.Archive();

        await environment.OrganizationDatabase.SeedAsync(db => db.Teams.Add(team));
        await environment.RegistrationsDatabase.SeedAsync(db =>
            db.TicketedEvents.AddRange(active, cancelled, archived));
    }
}
