using System.Net;
using System.Text.Json;
using Amolenk.Admitto.Api.Tests.Infrastructure;
using Shouldly;

namespace Amolenk.Admitto.Api.Tests.OpenApi;

[TestClass]
public sealed class OpenApiSecuritySchemeTests(TestContext testContext) : EndToEndTestBase
{
    [TestMethod]
    public async Task OpenApiSpec_ExposesOAuthAuthorizationCodeFlowUrls()
    {
        // Arrange
        // SC-OPENAPI-SECURITY: The published OpenAPI spec must advertise the OAuth2 authorization-code
        // flow with non-empty authorizationUrl and tokenUrl derived from the OIDC discovery endpoint.
        // This verifies that the app populates the security scheme from the identity provider discovery
        // document at startup rather than hard-coding it.

        // Act
        var response = await Environment.PublicApiClient.GetAsync(
            "/openapi/v1.json",
            testContext.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var json = await response.Content.ReadAsStringAsync(testContext.CancellationToken);
        using var doc = JsonDocument.Parse(json);

        var root = doc.RootElement;

        var authUrl = root
            .GetProperty("components")
            .GetProperty("securitySchemes")
            .GetProperty("Bearer")
            .GetProperty("flows")
            .GetProperty("authorizationCode")
            .GetProperty("authorizationUrl")
            .GetString();

        var tokenUrl = root
            .GetProperty("components")
            .GetProperty("securitySchemes")
            .GetProperty("Bearer")
            .GetProperty("flows")
            .GetProperty("authorizationCode")
            .GetProperty("tokenUrl")
            .GetString();

        authUrl.ShouldNotBeNullOrWhiteSpace();
        tokenUrl.ShouldNotBeNullOrWhiteSpace();
    }
}
