using Amolenk.Admitto.Api.Auth;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;
using Amolenk.Admitto.Testing.Builders.Organization.Domain;
using ExternalUserIdVO = Amolenk.Admitto.Core.Organization.Domain.ValueObjects.ExternalUserId;
using TeamIdVO = Amolenk.Admitto.Core.Shared.Kernel.ValueObjects.TeamId;
using UserIdVO = Amolenk.Admitto.Core.Organization.Domain.ValueObjects.UserId;
using TicketedEventIdVO = Amolenk.Admitto.Core.Shared.Kernel.ValueObjects.TicketedEventId;

namespace Amolenk.Admitto.Core.IntegrationTests.Organization.Auth;

internal sealed class UserContextResolverFixture
{
    public const string UserEmail = "alice@example.com";
    public const string ExternalUserId = "auth0|abc123";
    public const string DisplayName = "Alice";

    public Guid UserId { get; private set; }
    public Guid TeamId { get; private set; }
    public Guid EventId { get; private set; }

    public async ValueTask SeedUserWithoutExternalIdAsync(IntegrationTestEnvironment environment)
    {
        var user = new UserBuilder()
            .WithEmailAddress(EmailAddress.From(UserEmail))
            .Build();

        await environment.OrganizationDatabase.SeedAsync(dbContext =>
        {
            dbContext.Users.Add(user);
        });

        UserId = user.Id.Value;
    }

    public async ValueTask SeedUserWithExternalIdAsync(IntegrationTestEnvironment environment)
    {
        var user = new UserBuilder()
            .WithEmailAddress(EmailAddress.From(UserEmail))
            .Build();

        user.AssignExternalUserId(ExternalUserIdVO.From(ExternalUserId));

        await environment.OrganizationDatabase.SeedAsync(dbContext =>
        {
            dbContext.Users.Add(user);
        });

        UserId = user.Id.Value;
    }

    public async ValueTask SeedUserWithTeamMembershipAsync(
        IntegrationTestEnvironment environment,
        TeamMembershipRole role = TeamMembershipRole.Crew)
    {
        var team = new TeamBuilder().Build();
        var teamId = TeamIdVO.From(team.Id.Value);

        var user = new UserBuilder()
            .WithEmailAddress(EmailAddress.From(UserEmail))
            .WithMembership(teamId, role)
            .Build();

        user.AssignExternalUserId(ExternalUserIdVO.From(ExternalUserId));

        await environment.OrganizationDatabase.SeedAsync(dbContext =>
        {
            dbContext.Teams.Add(team);
            dbContext.Users.Add(user);
        });

        UserId = user.Id.Value;
        TeamId = team.Id.Value;
    }

    public async ValueTask SeedUserWithTeamAndEventAsync(
        IntegrationTestEnvironment environment,
        TeamMembershipRole role = TeamMembershipRole.Crew)
    {
        var team = new TeamBuilder().Build();
        var teamId = TeamIdVO.From(team.Id.Value);

        var creationRequest = team.RequestEventCreation(UserIdVO.New(), DateTimeOffset.UtcNow);
        var eventId = TicketedEventIdVO.New();
        team.RegisterEventCreated(creationRequest.Id, eventId, DateTimeOffset.UtcNow);

        var user = new UserBuilder()
            .WithEmailAddress(EmailAddress.From(UserEmail))
            .WithMembership(teamId, role)
            .Build();

        user.AssignExternalUserId(ExternalUserIdVO.From(ExternalUserId));

        await environment.OrganizationDatabase.SeedAsync(dbContext =>
        {
            dbContext.Teams.Add(team);
            dbContext.Users.Add(user);
        });

        UserId = user.Id.Value;
        TeamId = team.Id.Value;
        EventId = eventId.Value;
    }

    public async ValueTask SeedAdminWithoutMembershipsAsync(IntegrationTestEnvironment environment)
    {
        var user = new UserBuilder()
            .WithEmailAddress(EmailAddress.From(UserEmail))
            .WithIsAdmin()
            .Build();

        user.AssignExternalUserId(ExternalUserIdVO.From(ExternalUserId));

        await environment.OrganizationDatabase.SeedAsync(dbContext =>
        {
            dbContext.Users.Add(user);
        });

        UserId = user.Id.Value;
    }

    public UserContextResolver CreateResolver(IntegrationTestEnvironment environment)
        => new(
            environment.OrganizationDatabase.Context,
            new DbContextUnitOfWork(environment.OrganizationDatabase.Context));
}
