using System.Net;
using Amolenk.Admitto.Api.Tests.Infrastructure;
using Shouldly;

namespace Amolenk.Admitto.Api.Tests.Registrations.ReconfirmPolicy;

[TestClass]
public sealed class RemoveReconfirmPolicyTest(TestContext testContext) : EndToEndTestBase
{
    [TestMethod]
    public async Task RemoveReconfirmPolicy_Exists_Returns204()
    {
        // Arrange
        var fixture = PolicyEndpointFixture.WithExistingReconfirmPolicy();
        await fixture.SetupAsync(Environment);

        // Act
        var response = await Environment.ApiClient.DeleteAsync(
            PolicyEndpointFixture.ReconfirmPolicyRoute,
            testContext.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }
}
