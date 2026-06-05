using Amolenk.Admitto.Api.Tests.Infrastructure.Hosting;
using Amolenk.Admitto.Core.Registrations.Domain.Entities;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;
using Amolenk.Admitto.Testing.Builders.Organization.Application;

namespace Amolenk.Admitto.Api.Tests.Registrations.PolicyConstraints;

internal sealed class PolicyConstraintsFixture
{
    public static readonly DateTimeOffset EventStartsAt = DateTimeOffset.UtcNow.AddDays(60);
    public static readonly DateTimeOffset EventEndsAt = DateTimeOffset.UtcNow.AddDays(61);

    public Guid TeamId { get; private set; }
    public Guid EventId { get; private set; }

    public string RegistrationPolicyRoute => $"/admin/teams/{TeamId}/events/{EventId}/registration-policy";
    public string ReconfirmPolicyRoute => $"/admin/teams/{TeamId}/events/{EventId}/reconfirm-policy";

    private PolicyConstraintsFixture() { }

    public static PolicyConstraintsFixture WithActiveEvent() => new();

    public async ValueTask SetupAsync(EndToEndTestEnvironment environment)
    {
        var team = new TeamBuilder().Build();
        var eventId = TicketedEventId.New();

        TeamId = team.Id.Value;
        EventId = eventId.Value;

        var ticketedEvent = TicketedEvent.Create(
            CreationRequestId.From(Guid.NewGuid()),
            eventId,
            team.Id,
            EventName.From("Constraints Conf"),
            AbsoluteUrl.From("https://example.com"),
            AbsoluteUrl.From("https://tickets.example.com"),
            EventStartsAt,
            EventEndsAt,
            TimeZoneId.From("UTC"));

        await environment.OrganizationDatabase.SeedAsync(db => db.Teams.Add(team));
        await environment.RegistrationsDatabase.SeedAsync(db => db.TicketedEvents.Add(ticketedEvent));
    }
}
