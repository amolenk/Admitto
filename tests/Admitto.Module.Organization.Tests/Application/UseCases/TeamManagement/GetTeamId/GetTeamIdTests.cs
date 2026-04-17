using Amolenk.Admitto.Module.Organization.Application.UseCases.TeamManagement.GetTeamId;
using Amolenk.Admitto.Module.Organization.Tests.Application.Infrastructure;
using Amolenk.Admitto.Module.Shared.Kernel.ErrorHandling;

namespace Amolenk.Admitto.Module.Organization.Tests.Application.UseCases.TeamManagement.GetTeamId;

[TestClass]
public sealed class GetTeamIdTests(TestContext testContext) : AspireIntegrationTestBase
{
    [TestMethod]
    public async ValueTask GetTeamId_TeamExists_ReturnsTeamId()
    {
        // Arrange
        var fixture = GetTeamIdFixture.TeamExists();
        await fixture.SetupAsync(Environment);

        var query = new GetTeamIdQuery(fixture.TeamSlug);
        var sut = new GetTeamIdHandler(Environment.Database.Context);

        // Act
        var result = await sut.HandleAsync(query, testContext.CancellationToken);

        // Assert
        result.ShouldBe(fixture.TeamId);
    }

    [TestMethod]
    public async ValueTask GetTeamId_TeamDoesNotExist_ThrowsBusinessRuleViolation()
    {
        // Arrange
        var query = new GetTeamIdQuery("nonexistent");
        var sut = new GetTeamIdHandler(Environment.Database.Context);

        // Act & Assert
        await Should.ThrowAsync<BusinessRuleViolationException>(
            sut.HandleAsync(query, testContext.CancellationToken).AsTask());
    }
}
