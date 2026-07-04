using Amolenk.Admitto.Core.Organization.Domain.Entities;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;
using Amolenk.Admitto.Testing.Builders.Organization.Application;

namespace Amolenk.Admitto.Core.IntegrationTests.Organization.Application.UseCases.Teams.GetTeams;

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

    public static GetTeamsFixture AdminListsTeamsWithMixedCaseNames() => new();

    public static GetTeamsFixture UserListsOwnTeamsWithMixedCaseNames() => new();

    public async ValueTask SetupAdminTeamsAsync(IntegrationTestEnvironment environment)
    {
        var acme = new TeamBuilder().WithName("Acme Events").Build();
        var beta = new TeamBuilder().WithName("Beta Events").Build();
        var retired = new TeamBuilder().WithName("Retired Team").AsArchived().Build();

        await environment.OrganizationDatabase.SeedAsync(dbContext =>
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
        var acme = new TeamBuilder().WithName("Acme Events").Build();
        var beta = _includeArchivedMembership
            ? new TeamBuilder().WithName("Beta Events").AsArchived().Build()
            : new TeamBuilder().WithName("Beta Events").Build();
        var gamma = new TeamBuilder().WithName("Gamma Events").Build();

        var user = User.Create(EmailAddress.From("member@example.com"));
        user.AddTeamMembership(acme.Id, TeamMembershipRole.Owner);
        user.AddTeamMembership(beta.Id, TeamMembershipRole.Organizer);

        await environment.OrganizationDatabase.SeedAsync(dbContext =>
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

    // SC: teams listed in alphabetical order (case-insensitive)
    public async ValueTask SetupAdminTeamsWithMixedCaseNamesAsync(IntegrationTestEnvironment environment)
    {
        var zebra = new TeamBuilder().WithName("Zebra Events").Build();
        var acme = new TeamBuilder().WithName("acme").Build();
        var beta = new TeamBuilder().WithName("Beta Corp").Build();

        await environment.OrganizationDatabase.SeedAsync(dbContext =>
        {
            dbContext.Teams.Add(zebra);
            dbContext.Teams.Add(acme);
            dbContext.Teams.Add(beta);
        });
    }

    public async ValueTask SetupMemberTeamsWithMixedCaseNamesAsync(IntegrationTestEnvironment environment)
    {
        var zebra = new TeamBuilder().WithName("Zebra Events").Build();
        var acme = new TeamBuilder().WithName("acme").Build();
        var beta = new TeamBuilder().WithName("Beta Corp").Build();

        var user = User.Create(EmailAddress.From("member@example.com"));
        user.AddTeamMembership(zebra.Id, TeamMembershipRole.Owner);
        user.AddTeamMembership(acme.Id, TeamMembershipRole.Owner);
        user.AddTeamMembership(beta.Id, TeamMembershipRole.Owner);

        await environment.OrganizationDatabase.SeedAsync(dbContext =>
        {
            dbContext.Teams.Add(zebra);
            dbContext.Teams.Add(acme);
            dbContext.Teams.Add(beta);
            dbContext.Users.Add(user);
        });

        UserId = user.Id.Value;
    }
}
