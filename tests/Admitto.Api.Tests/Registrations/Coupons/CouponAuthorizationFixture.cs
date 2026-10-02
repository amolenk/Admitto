using Amolenk.Admitto.Api.Tests.Infrastructure.Hosting;
using Amolenk.Admitto.Core.Registrations.Domain.Entities;
using Amolenk.Admitto.Core.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Application.Persistence;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;
using TeamBuilder = Amolenk.Admitto.Testing.Builders.Organization.Application.TeamBuilder;
using DomainTeamId = Amolenk.Admitto.Core.Shared.Kernel.ValueObjects.TeamId;

namespace Amolenk.Admitto.Api.Tests.Registrations.Coupons;

internal sealed class CouponAuthorizationFixture
{
    public Guid TeamId { get; private set; }
    public Guid EventId { get; private set; }
    public TicketTypeId TicketTypeId { get; } = TicketTypeId.New();
    public Guid CouponId { get; private set; }

    public string CouponsRoute => $"/admin/teams/{TeamId}/events/{EventId}/coupons";
    public string CouponRoute => $"{CouponsRoute}/{CouponId}";

    private CouponAuthorizationFixture() { }

    public static CouponAuthorizationFixture BobIsCrewMember() => new();

    public static CouponAuthorizationFixture BobIsOwnerOfDifferentTeam() => new();

    public static CouponAuthorizationFixture NoTeamMembers() => new();

    public async ValueTask SetupAsync(EndToEndTestEnvironment environment)
    {
        var team = new TeamBuilder().Build();
        TeamId = team.Id.Value;

        await SeedRegistrationsDataAsync(environment, team.Id);

        var bob = await environment.OrganizationDatabase.Context.Users.GetAsync(u =>
            u.EmailAddress == EmailAddress.From("bob@example.com"));

        await environment.OrganizationDatabase.SeedAsync(dbContext =>
        {
            bob.AddTeamMembership(team.Id, TeamMembershipRole.Crew);
            dbContext.Teams.Add(team);
        });
    }

    public async ValueTask SetupWithOtherTeamMembershipAsync(EndToEndTestEnvironment environment)
    {
        var requestedTeam = new TeamBuilder().Build();
        var otherTeam = new TeamBuilder().Build();
        TeamId = requestedTeam.Id.Value;

        await SeedRegistrationsDataAsync(environment, requestedTeam.Id);

        var bob = await environment.OrganizationDatabase.Context.Users.GetAsync(u =>
            u.EmailAddress == EmailAddress.From("bob@example.com"));

        await environment.OrganizationDatabase.SeedAsync(dbContext =>
        {
            bob.AddTeamMembership(otherTeam.Id, TeamMembershipRole.Owner);
            dbContext.Teams.AddRange(requestedTeam, otherTeam);
        });
    }

    public async ValueTask SetupTeamOnlyAsync(EndToEndTestEnvironment environment)
    {
        var team = new TeamBuilder().Build();
        TeamId = team.Id.Value;

        await SeedRegistrationsDataAsync(environment, team.Id);

        await environment.OrganizationDatabase.SeedAsync(dbContext =>
        {
            dbContext.Teams.Add(team);
        });
    }

    private async ValueTask SeedRegistrationsDataAsync(EndToEndTestEnvironment environment, DomainTeamId teamId)
    {
        var eventId = TicketedEventId.New();
        EventId = eventId.Value;

        var ticketedEvent = TicketedEvent.Create(
            CreationRequestId.From(Guid.NewGuid()),
            eventId,
            teamId,
            EventName.From("DevConf"),
            AbsoluteUrl.From("https://example.com"),
            AbsoluteUrl.From("https://tickets.example.com"),
            DateTimeOffset.UtcNow.AddDays(60),
            DateTimeOffset.UtcNow.AddDays(61),
            TimeZoneId.From("UTC"));

        var catalog = TicketCatalog.Create(eventId, teamId);
        catalog.AddTicketType(TicketTypeId, TicketTypeName.From("General Admission"), [], 100);

        var coupon = Coupon.Create(
            eventId,
            teamId,
            EmailAddress.From("invitee@example.com"),
            [TicketTypeId],
            DateTimeOffset.UtcNow.AddDays(30),
            bypassRegistrationWindow: false,
            [new TicketTypeInfo(TicketTypeId)],
            DateTimeOffset.UtcNow);
        CouponId = coupon.Id.Value;

        await environment.RegistrationsDatabase.SeedAsync(db =>
        {
            db.TicketedEvents.Add(ticketedEvent);
            db.TicketCatalogs.Add(catalog);
            db.Coupons.Add(coupon);
        });
    }
}
