using Amolenk.Admitto.Module.Organization.Tests.Application.Infrastructure;
using Amolenk.Admitto.Module.Organization.Domain.ValueObjects;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;
using NSubstitute;
using Quartz;

namespace Amolenk.Admitto.Module.Organization.Tests.Application.Jobs;

[TestClass]
public sealed class DeprovisionUserIdpJobTests(TestContext testContext) : AspireIntegrationTestBase
{
    [TestMethod]
    public async ValueTask SC013_DeprovisionUserIdpJob_GracePeriodExpired_DeprovisionsUser()
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

        await Environment.Database.AssertAsync(async dbContext =>
        {
            var user = await dbContext.Users.FindAsync(
                [UserId.From(fixture.UserId)], testContext.CancellationToken);

            user.ShouldNotBeNull();
            user.ExternalUserId.ShouldBeNull();
            user.DeprovisionAfter.ShouldBeNull();
        });
    }
}
