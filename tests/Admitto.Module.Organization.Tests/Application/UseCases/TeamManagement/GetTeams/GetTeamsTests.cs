using Amolenk.Admitto.Module.Organization.Tests.Application.Infrastructure;
using Amolenk.Admitto.Module.Organization.Application.UseCases.TeamManagement.GetTeams;

namespace Amolenk.Admitto.Module.Organization.Tests.Application.UseCases.TeamManagement.GetTeams;

[TestClass]
public sealed class GetTeamsTests(TestContext testContext) : AspireIntegrationTestBase
{
    [TestMethod]
    public async ValueTask SC006_GetTeams_AdminListsAllTeams_ReturnsOnlyActiveTeams()
    {
        // Arrange
        // SC-006: Given teams "acme" (active), "beta" (active), and "retired" (archived),
        // when an admin lists all teams, only "acme" and "beta" are returned.
        var fixture = GetTeamsFixture.AdminListsAllActiveTeams();
        await fixture.SetupAdminTeamsAsync(Environment);

        var query = new GetTeamsQuery(Guid.NewGuid(), CallerIsAdmin: true);
        var sut = new GetTeamsHandler(Environment.Database.Context);

        // Act
        var result = await sut.HandleAsync(query, testContext.CancellationToken);

        // Assert
        result.ShouldNotBeNull();
        result.Count.ShouldBe(2);
        result.ShouldContain(t => t.Slug == "acme");
        result.ShouldContain(t => t.Slug == "beta");
        result.ShouldNotContain(t => t.Slug == "retired");
    }

    [TestMethod]
    public async ValueTask SC012_GetTeams_NonAdminListsOwnTeams_ReturnsOnlyMemberTeams()
    {
        // Arrange
        // SC-012: Given user is a member of "acme" and "beta" but not "gamma",
        // when they list their teams, only "acme" and "beta" are returned.
        var fixture = GetTeamsFixture.UserListsOwnActiveTeams();
        await fixture.SetupMemberTeamsAsync(Environment);

        var query = new GetTeamsQuery(fixture.UserId, CallerIsAdmin: false);
        var sut = new GetTeamsHandler(Environment.Database.Context);

        // Act
        var result = await sut.HandleAsync(query, testContext.CancellationToken);

        // Assert
        result.ShouldNotBeNull();
        result.Count.ShouldBe(2);
        result.ShouldContain(t => t.Slug == "acme");
        result.ShouldContain(t => t.Slug == "beta");
        result.ShouldNotContain(t => t.Slug == "gamma");
    }

    [TestMethod]
    public async ValueTask SC013_GetTeams_NonAdminWithArchivedMembership_ExcludesArchivedTeam()
    {
        // Arrange
        // SC-013: Given user is a member of "acme" (active) and "beta" (archived),
        // when they list their teams, only "acme" is returned.
        var fixture = GetTeamsFixture.UserListsOwnTeamsWithArchivedMembership();
        await fixture.SetupMemberTeamsAsync(Environment);

        var query = new GetTeamsQuery(fixture.UserId, CallerIsAdmin: false);
        var sut = new GetTeamsHandler(Environment.Database.Context);

        // Act
        var result = await sut.HandleAsync(query, testContext.CancellationToken);

        // Assert
        result.ShouldNotBeNull();
        result.ShouldHaveSingleItem();
        result[0].Slug.ShouldBe("acme");
        result.ShouldNotContain(t => t.Slug == "beta");
    }
}
