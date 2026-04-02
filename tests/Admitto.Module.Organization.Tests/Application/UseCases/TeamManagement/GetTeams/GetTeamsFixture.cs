using Amolenk.Admitto.Module.Organization.Tests.Application.Builders;
using Amolenk.Admitto.Module.Organization.Tests.Application.Infrastructure.Hosting;
using Amolenk.Admitto.Module.Organization.Domain.Entities;
using Amolenk.Admitto.Module.Organization.Domain.ValueObjects;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Module.Organization.Tests.Application.UseCases.TeamManagement.GetTeams;

internal sealed class GetTeamsFixture
{
    // SC-006: admin lists all active teams
    public Guid ActiveTeamAcmeId { get; private set; }
    public Guid ActiveTeamBetaId { get; private set; }
    public Guid ArchivedTeamRetiredId { get; private set; }

    // SC-012/SC-013: member lists own teams
    public Guid UserId { get; private set; }
    public Guid MemberTeamAcmeId { get; private set; }
    public Guid MemberTeamBetaId { get; private set; }
    public Guid NonMemberTeamGammaId { get; private set; }

    private readonly bool _includeArchivedMembership;

    private GetTeamsFixture(bool includeArchivedMembership = false)
    {
        _includeArchivedMembership = includeArchivedMembership;
    }

    public static GetTeamsFixture AdminListsAllActiveTeams() => new();

    public static GetTeamsFixture UserListsOwnActiveTeams() => new();

    public static GetTeamsFixture UserListsOwnTeamsWithArchivedMembership() =>
        new(includeArchivedMembership: true);

    public async ValueTask SetupAdminTeamsAsync(IntegrationTestEnvironment environment)
    {
        var acme = new TeamBuilder().WithSlug("acme").WithName("Acme Events").Build();
        var beta = new TeamBuilder().WithSlug("beta").WithName("Beta Events").Build();
        var retired = new TeamBuilder().WithSlug("retired").WithName("Retired Team").AsArchived().Build();

        await environment.Database.SeedAsync(dbContext =>
        {
            dbContext.Teams.Add(acme);
            dbContext.Teams.Add(beta);
            dbContext.Teams.Add(retired);
        });

        ActiveTeamAcmeId = acme.Id.Value;
        ActiveTeamBetaId = beta.Id.Value;
        ArchivedTeamRetiredId = retired.Id.Value;
    }

    public async ValueTask SetupMemberTeamsAsync(IntegrationTestEnvironment environment)
    {
        var acme = new TeamBuilder().WithSlug("acme").WithName("Acme Events").Build();
        var beta = _includeArchivedMembership
            ? new TeamBuilder().WithSlug("beta").WithName("Beta Events").AsArchived().Build()
            : new TeamBuilder().WithSlug("beta").WithName("Beta Events").Build();
        var gamma = new TeamBuilder().WithSlug("gamma").WithName("Gamma Events").Build();

        var user = User.Create(EmailAddress.From("member@example.com"));
        user.AddTeamMembership(acme.Id, TeamMembershipRole.Crew);
        user.AddTeamMembership(beta.Id, TeamMembershipRole.Crew);

        await environment.Database.SeedAsync(dbContext =>
        {
            dbContext.Teams.Add(acme);
            dbContext.Teams.Add(beta);
            dbContext.Teams.Add(gamma);
            dbContext.Users.Add(user);
        });

        MemberTeamAcmeId = acme.Id.Value;
        MemberTeamBetaId = beta.Id.Value;
        NonMemberTeamGammaId = gamma.Id.Value;
        UserId = user.Id.Value;
    }
}
