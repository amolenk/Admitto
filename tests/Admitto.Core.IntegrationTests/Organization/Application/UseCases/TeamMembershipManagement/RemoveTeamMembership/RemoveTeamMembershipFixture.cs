using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;
using Amolenk.Admitto.Testing.Builders.Organization.Domain;

namespace Amolenk.Admitto.Core.IntegrationTests.Organization.Application.UseCases.TeamMembershipManagement.RemoveTeamMembership;

internal sealed class RemoveTeamMembershipFixture
{
    public Guid TeamId { get; } = Guid.NewGuid();
    public Guid OtherTeamId { get; } = Guid.NewGuid();
    public string EmailAddress { get; } = "alice@example.com";
    public Guid UserId { get; private set; }

    private readonly bool _hasOtherMemberships;

    private RemoveTeamMembershipFixture(bool hasOtherMemberships)
    {
        _hasOtherMemberships = hasOtherMemberships;
    }

    public static RemoveTeamMembershipFixture MemberWithOtherTeams() => new(hasOtherMemberships: true);

    public static RemoveTeamMembershipFixture MemberInOnlyThisTeam() => new(hasOtherMemberships: false);

    public async ValueTask SetupAsync(IntegrationTestEnvironment environment)
    {
        var teamId = Amolenk.Admitto.Core.Shared.Kernel.ValueObjects.TeamId.From(TeamId);

        var builder = new UserBuilder()
            .WithEmailAddress(Amolenk.Admitto.Core.Shared.Kernel.ValueObjects.EmailAddress.From(EmailAddress))
            .WithMembership(teamId, TeamMembershipRole.Crew);

        if (_hasOtherMemberships)
        {
            builder = builder.WithMembership(
                Amolenk.Admitto.Core.Shared.Kernel.ValueObjects.TeamId.From(OtherTeamId),
                TeamMembershipRole.Owner);
        }

        var user = builder.Build();

        await environment.OrganizationDatabase.SeedAsync(dbContext =>
        {
            dbContext.Users.Add(user);
        });

        UserId = user.Id.Value;
    }
}
