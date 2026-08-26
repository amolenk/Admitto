using Amolenk.Admitto.Core.Organization.Application.UseCases.Teams.GetTeams;

namespace Amolenk.Admitto.Core.IntegrationTests.Organization.Application.UseCases.Teams.GetTeams;

[TestClass]
public sealed class GetTeamsTests(TestContext testContext) : AspireIntegrationTestBase
{
    // Given active teams "acme" and "beta" and an archived team "retired"
    // When an admin lists all teams
    // Then only the active teams are returned with full management permissions
    [TestMethod]
    public async ValueTask GetTeams_AdminListsAllTeams_ReturnsOnlyActiveTeams()
    {
        // Arrange
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

    // Given a user is a member of "acme" (with management permissions) and "beta" (without), but not "gamma"
    // When the user lists their own teams
    // Then only "acme" and "beta" are returned with their respective permissions
    [TestMethod]
    public async ValueTask GetTeams_NonAdminListsOwnTeams_ReturnsOnlyMemberTeams()
    {
        // Arrange
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

    // Given a user is a member of active team "acme" and archived team "beta"
    // When the user lists their own teams
    // Then only "acme" is returned
    [TestMethod]
    public async ValueTask GetTeams_NonAdminWithArchivedMembership_ExcludesArchivedTeam()
    {
        // Arrange
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

    // Given teams with mixed-case names "Zebra Events", "acme", and "Beta Corp"
    // When an admin lists all teams
    // Then they are returned case-insensitively in alphabetical order
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

    // Given the user's teams have mixed-case names "Zebra Events", "acme", and "Beta Corp"
    // When the user lists their own teams
    // Then they are returned case-insensitively in alphabetical order
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
