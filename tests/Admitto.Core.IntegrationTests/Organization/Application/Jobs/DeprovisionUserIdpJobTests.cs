using Amolenk.Admitto.Core.Organization.Domain.ValueObjects;
using NSubstitute;
using Quartz;

namespace Amolenk.Admitto.Core.IntegrationTests.Organization.Application.Jobs;

[TestClass]
public sealed class DeprovisionUserIdpJobTests(TestContext testContext) : AspireIntegrationTestBase
{
    [TestMethod]
    public async ValueTask DeprovisionUserIdpJob_GracePeriodExpired_DeprovisionsUser()
    {
        // Arrange
        // SC-013: Given alice has been removed from all teams and her grace period has
        // expired, when the deprovisioning job runs, her IdP account is deleted and
        // ExternalUserId + DeprovisionAfter are cleared.
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
