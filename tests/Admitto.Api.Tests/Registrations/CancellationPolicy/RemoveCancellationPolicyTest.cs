using System.Net;
using Amolenk.Admitto.Api.Tests.Infrastructure;
using Shouldly;

namespace Amolenk.Admitto.Api.Tests.Registrations.CancellationPolicy;

[TestClass]
public sealed class RemoveCancellationPolicyTest(TestContext testContext) : EndToEndTestBase
{
    [TestMethod]
    public async Task RemoveCancellationPolicy_Exists_Returns204()
    {
        // Arrange
        var fixture = PolicyEndpointFixture.WithExistingCancellationPolicy();
        await fixture.SetupAsync(Environment);

        // Act
        var response = await Environment.ApiClient.DeleteAsync(
            PolicyEndpointFixture.CancellationPolicyRoute,
            testContext.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }
}
