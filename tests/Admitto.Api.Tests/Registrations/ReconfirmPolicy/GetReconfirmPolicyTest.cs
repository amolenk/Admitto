using System.Net;
using System.Net.Http.Json;
using Amolenk.Admitto.Api.Tests.Infrastructure;
using Amolenk.Admitto.Module.Registrations.Application.UseCases.ReconfirmPolicy.GetReconfirmPolicy;
using Shouldly;

namespace Amolenk.Admitto.Api.Tests.Registrations.ReconfirmPolicy;

[TestClass]
public sealed class GetReconfirmPolicyTest(TestContext testContext) : EndToEndTestBase
{
    [TestMethod]
    public async Task GetReconfirmPolicy_Exists_Returns200WithDto()
    {
        // Arrange
        var fixture = PolicyEndpointFixture.WithExistingReconfirmPolicy();
        await fixture.SetupAsync(Environment);

        // Act
        var response = await Environment.ApiClient.GetAsync(
            PolicyEndpointFixture.ReconfirmPolicyRoute,
            testContext.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var dto = await response.Content.ReadFromJsonAsync<ReconfirmPolicyDto>(
            cancellationToken: testContext.CancellationToken);
        dto.ShouldNotBeNull();
        dto.OpensAt.ShouldBe(fixture.ReconfirmOpensAt, TimeSpan.FromSeconds(1));
        dto.ClosesAt.ShouldBe(fixture.ReconfirmClosesAt, TimeSpan.FromSeconds(1));
        dto.CadenceDays.ShouldBe(fixture.ReconfirmCadenceDays);
    }

    [TestMethod]
    public async Task GetReconfirmPolicy_NotFound_Returns404()
    {
        // Arrange
        var fixture = PolicyEndpointFixture.ActiveEvent();
        await fixture.SetupAsync(Environment);

        // Act
        var response = await Environment.ApiClient.GetAsync(
            PolicyEndpointFixture.ReconfirmPolicyRoute,
            testContext.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }
}
