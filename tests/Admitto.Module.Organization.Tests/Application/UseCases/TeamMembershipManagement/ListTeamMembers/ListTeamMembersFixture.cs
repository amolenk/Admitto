using Amolenk.Admitto.Module.Organization.Tests.Application.Infrastructure.Hosting;
using Amolenk.Admitto.Module.Organization.Domain.Tests.Builders;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Module.Organization.Tests.Application.UseCases.TeamMembershipManagement.ListTeamMembers;

internal sealed class ListTeamMembersFixture
{
    public Guid TeamId { get; } = Guid.NewGuid();

    private readonly bool _withMembers;

    private ListTeamMembersFixture(bool withMembers)
    {
        _withMembers = withMembers;
    }

    public static ListTeamMembersFixture TeamWithMembers() => new(withMembers: true);

    public static ListTeamMembersFixture EmptyTeam() => new(withMembers: false);

    public async ValueTask SetupAsync(IntegrationTestEnvironment environment)
    {
        if (!_withMembers) return;

        var teamId = Amolenk.Admitto.Module.Shared.Kernel.ValueObjects.TeamId.From(TeamId);
        var alice = new UserBuilder()
            .WithEmailAddress(EmailAddress.From("alice@example.com"))
            .WithMembership(teamId, TeamMembershipRole.Owner)
            .Build();
        var bob = new UserBuilder()
            .WithEmailAddress(EmailAddress.From("bob@example.com"))
            .WithMembership(teamId, TeamMembershipRole.Crew)
            .Build();

        await environment.Database.SeedAsync(dbContext =>
        {
            dbContext.Users.Add(alice);
            dbContext.Users.Add(bob);
        });
    }
}
