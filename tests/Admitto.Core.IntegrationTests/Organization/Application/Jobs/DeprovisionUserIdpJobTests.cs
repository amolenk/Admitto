using Amolenk.Admitto.Core.Organization.Domain.ValueObjects;
using NSubstitute;
using Quartz;

namespace Amolenk.Admitto.Core.IntegrationTests.Organization.Application.Jobs;

[TestClass]
public sealed class DeprovisionUserIdpJobTests(TestContext testContext) : AspireIntegrationTestBase
{
    // Given a user removed from all teams whose deprovisioning grace period has expired
    // When the deprovisioning job runs
    // Then the user's IdP account is deleted and their external user id and grace period are cleared
    [TestMethod]
    public async ValueTask DeprovisionUserIdpJob_GracePeriodExpired_DeprovisionsUser()
    {
        // Arrange
        var fixture = DeprovisionUserIdpJobFixture.GracePeriodExpired();
        await fixture.SetupAsync(Environment);

        var jobContext = Substitute.For<IJobExecutionContext>();
        jobContext.CancellationToken.Returns(testContext.CancellationToken);

        var sut = fixture.CreateJob(Environment);

        // Act
        await sut.Execute(jobContext);

        // Assert
        await fixture.ExternalUserDirectory.Received(1)
            .DeleteUserAsync(fixture.ExternalUserId, testContext.CancellationToken);

        await Environment.OrganizationDatabase.AssertAsync(async dbContext =>
        {
            var user = await dbContext.Users.FindAsync(
                [UserId.From(fixture.UserId)], testContext.CancellationToken);

            user.ShouldNotBeNull();
            user.ExternalUserId.ShouldBeNull();
            user.DeprovisionAfter.ShouldBeNull();
        });
    }
}
