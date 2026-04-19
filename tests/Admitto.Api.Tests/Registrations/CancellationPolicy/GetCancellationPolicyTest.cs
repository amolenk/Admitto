using System.Net;
using System.Net.Http.Json;
using Amolenk.Admitto.Api.Tests.Infrastructure;
using Amolenk.Admitto.Module.Registrations.Application.UseCases.CancellationPolicy.GetCancellationPolicy;
using Shouldly;

namespace Amolenk.Admitto.Api.Tests.Registrations.CancellationPolicy;

[TestClass]
public sealed class GetCancellationPolicyTest(TestContext testContext) : EndToEndTestBase
{
    [TestMethod]
    public async Task GetCancellationPolicy_Exists_Returns200WithDto()
    {
        // Arrange
        var fixture = PolicyEndpointFixture.WithExistingCancellationPolicy();
        await fixture.SetupAsync(Environment);

        // Act
        var response = await Environment.ApiClient.GetAsync(
            PolicyEndpointFixture.CancellationPolicyRoute,
            testContext.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var dto = await response.Content.ReadFromJsonAsync<CancellationPolicyDto>(
            cancellationToken: testContext.CancellationToken);
        dto.ShouldNotBeNull();
        dto.LateCancellationCutoff.ShouldBe(fixture.CancellationCutoff, TimeSpan.FromSeconds(1));
    }

    [TestMethod]
    public async Task GetCancellationPolicy_NotFound_Returns404()
    {
        // Arrange
        var fixture = PolicyEndpointFixture.ActiveEvent();
        await fixture.SetupAsync(Environment);

        // Act
        var response = await Environment.ApiClient.GetAsync(
            PolicyEndpointFixture.CancellationPolicyRoute,
            testContext.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }
}
