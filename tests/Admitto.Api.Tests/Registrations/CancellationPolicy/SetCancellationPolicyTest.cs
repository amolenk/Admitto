using System.Net;
using System.Net.Http.Json;
using Amolenk.Admitto.Api.Tests.Infrastructure;
using Amolenk.Admitto.Testing.Infrastructure.Assertions;
using Shouldly;

namespace Amolenk.Admitto.Api.Tests.Registrations.CancellationPolicy;

[TestClass]
public sealed class SetCancellationPolicyTest(TestContext testContext) : EndToEndTestBase
{
    [TestMethod]
    public async Task SetCancellationPolicy_HappyFlow_Returns200()
    {
        // Arrange
        var fixture = PolicyEndpointFixture.ActiveEvent();
        await fixture.SetupAsync(Environment);

        var request = new { LateCancellationCutoff = DateTimeOffset.UtcNow.AddDays(7) };

        // Act
        var response = await Environment.ApiClient.PutAsJsonAsync(
            PolicyEndpointFixture.CancellationPolicyRoute,
            request,
            cancellationToken: testContext.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [TestMethod]
    public async Task SetCancellationPolicy_CancelledEvent_ReturnsError()
    {
        // Arrange
        var fixture = PolicyEndpointFixture.CancelledEvent();
        await fixture.SetupAsync(Environment);

        var request = new { LateCancellationCutoff = DateTimeOffset.UtcNow.AddDays(7) };

        // Act
        var response = await Environment.ApiClient.PutAsJsonAsync(
            PolicyEndpointFixture.CancellationPolicyRoute,
            request,
            cancellationToken: testContext.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        response.Content.ShouldBeProblemDetails()
            .ShouldHaveErrorCode("lifecycle_guard.event_not_active");
    }
}
