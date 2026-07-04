using Amolenk.Admitto.Core.Organization.Application.UseCases.Teams.GetTeams;

namespace Amolenk.Admitto.Core.IntegrationTests.Organization.Application.UseCases.Teams.GetTeams;

[TestClass]
public sealed class GetTeamsTests(TestContext testContext) : AspireIntegrationTestBase
{
    [TestMethod]
    public async ValueTask GetTeams_AdminListsAllTeams_ReturnsOnlyActiveTeams()
    {
        // Arrange
        // SC-006: Given teams "acme" (active), "beta" (active), and "retired" (archived),
        // when an admin lists all teams, only "acme" and "beta" are returned.
        var fixture = GetTeamsFixture.AdminListsAllActiveTeams();
        await fixture.SetupAdminTeamsAsync(Environment);

        var query = new GetTeamsQuery(Guid.NewGuid(), CallerIsAdmin: true);
        var sut = new GetTeamsHandler(Environment.OrganizationDatabase.Context);

        // Act
        var result = await sut.HandleAsync(query, testContext.CancellationToken);

        // Assert
        result.ShouldNotBeNull();
        result.Count.ShouldBe(2);
        result.ShouldContain(t => t.Name == "Acme Events" && t.CanManageTeamSettings && t.CanCreateEvents);
        result.ShouldContain(t => t.Name == "Beta Events" && t.CanManageTeamSettings && t.CanCreateEvents);
        result.ShouldNotContain(t => t.Name == "Retired Team");
    }

    [TestMethod]
    public async ValueTask GetTeams_NonAdminListsOwnTeams_ReturnsOnlyMemberTeams()
    {
        // Arrange
        // SC-012: Given user is a member of "acme" and "beta" but not "gamma",
        // when they list their teams, only "acme" and "beta" are returned.
        var fixture = GetTeamsFixture.UserListsOwnActiveTeams();
        await fixture.SetupMemberTeamsAsync(Environment);

        var query = new GetTeamsQuery(fixture.UserId, CallerIsAdmin: false);
        var sut = new GetTeamsHandler(Environment.OrganizationDatabase.Context);

        // Act
        var result = await sut.HandleAsync(query, testContext.CancellationToken);

        // Assert
        result.ShouldNotBeNull();
        result.Count.ShouldBe(2);
        result.ShouldContain(t => t.Name == "Acme Events" && t.CanManageTeamSettings && t.CanCreateEvents);
        result.ShouldContain(t => t.Name == "Beta Events" && !t.CanManageTeamSettings && !t.CanCreateEvents);
        result.ShouldNotContain(t => t.Name == "Gamma Events");
    }

    [TestMethod]
    public async ValueTask GetTeams_NonAdminWithArchivedMembership_ExcludesArchivedTeam()
    {
        // Arrange
        // SC-013: Given user is a member of "acme" (active) and "beta" (archived),
        // when they list their teams, only "acme" is returned.
        var fixture = GetTeamsFixture.UserListsOwnTeamsWithArchivedMembership();
        await fixture.SetupMemberTeamsAsync(Environment);

        var query = new GetTeamsQuery(fixture.UserId, CallerIsAdmin: false);
        var sut = new GetTeamsHandler(Environment.OrganizationDatabase.Context);

        // Act
        var result = await sut.HandleAsync(query, testContext.CancellationToken);

        // Assert
        result.ShouldNotBeNull();
        result.ShouldHaveSingleItem();
        result[0].Name.ShouldBe("Acme Events");
        result[0].CanManageTeamSettings.ShouldBeTrue();
        result[0].CanCreateEvents.ShouldBeTrue();
        result.ShouldNotContain(t => t.Name == "Beta Events");
    }

    [TestMethod]
    public async ValueTask GetTeams_AdminListsAllTeams_ReturnsInAlphabeticalOrder()
    {
        // Arrange
        // Teams "Zebra Events", "acme", "Beta Corp" are returned case-insensitively
        // alphabetical: "acme", "Beta Corp", "Zebra Events".
        var fixture = GetTeamsFixture.AdminListsTeamsWithMixedCaseNames();
        await fixture.SetupAdminTeamsWithMixedCaseNamesAsync(Environment);

        var query = new GetTeamsQuery(Guid.NewGuid(), CallerIsAdmin: true);
        var sut = new GetTeamsHandler(Environment.OrganizationDatabase.Context);

        // Act
        var result = await sut.HandleAsync(query, testContext.CancellationToken);

        // Assert
        result.Count.ShouldBe(3);
        result[0].Name.ShouldBe("acme");
        result[1].Name.ShouldBe("Beta Corp");
        result[2].Name.ShouldBe("Zebra Events");
    }

    [TestMethod]
    public async ValueTask GetTeams_NonAdminListsOwnTeams_ReturnsInAlphabeticalOrder()
    {
        // Arrange
        // Teams "Zebra Events", "acme", "Beta Corp" are returned case-insensitively
        // alphabetical: "acme", "Beta Corp", "Zebra Events".
        var fixture = GetTeamsFixture.UserListsOwnTeamsWithMixedCaseNames();
        await fixture.SetupMemberTeamsWithMixedCaseNamesAsync(Environment);

        var query = new GetTeamsQuery(fixture.UserId, CallerIsAdmin: false);
        var sut = new GetTeamsHandler(Environment.OrganizationDatabase.Context);

        // Act
        var result = await sut.HandleAsync(query, testContext.CancellationToken);

        // Assert
        result.Count.ShouldBe(3);
        result[0].Name.ShouldBe("acme");
        result[1].Name.ShouldBe("Beta Corp");
        result[2].Name.ShouldBe("Zebra Events");
    }
}
