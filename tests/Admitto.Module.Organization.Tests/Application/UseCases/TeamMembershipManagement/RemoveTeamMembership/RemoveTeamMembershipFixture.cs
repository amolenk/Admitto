using Amolenk.Admitto.Module.Organization.Tests.Application.Infrastructure.Hosting;
using Amolenk.Admitto.Module.Organization.Domain.Tests.Builders;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Module.Organization.Tests.Application.UseCases.TeamMembershipManagement.RemoveTeamMembership;

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
        var teamId = Amolenk.Admitto.Module.Shared.Kernel.ValueObjects.TeamId.From(TeamId);

        var builder = new UserBuilder()
            .WithEmailAddress(Amolenk.Admitto.Module.Shared.Kernel.ValueObjects.EmailAddress.From(EmailAddress))
            .WithMembership(teamId, TeamMembershipRole.Crew);

        if (_hasOtherMemberships)
        {
            builder = builder.WithMembership(
                Amolenk.Admitto.Module.Shared.Kernel.ValueObjects.TeamId.From(OtherTeamId),
                TeamMembershipRole.Owner);
        }

        var user = builder.Build();

        await environment.Database.SeedAsync(dbContext =>
        {
            dbContext.Users.Add(user);
        });

        UserId = user.Id.Value;
    }
}
