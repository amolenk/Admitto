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
        var response = await Environment.AnonymousApiClient.GetAsync(
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

    [TestMethod]
    public async Task OpenApiSpec_PartnerEndpointsRequireApiKeyOnlyByDefault()
    {
        // Act
        using var doc = await GetOpenApiDocumentAsync();

        // Assert
        var root = doc.RootElement;
        root.TryGetProperty("security", out _).ShouldBeFalse();

        var securitySchemes = root.GetProperty("components").GetProperty("securitySchemes");
        securitySchemes.GetProperty("ApiKey").GetProperty("name").GetString().ShouldBe("X-Api-Key");
        securitySchemes.GetProperty("ApiKey").GetProperty("in").GetString().ShouldBe("header");

        GetSecuritySchemeNames(root, "/api/events/{eventSlug}/otp/request", "post")
            .ShouldBe(["ApiKey"]);
        GetSecuritySchemeNames(root, "/api/events/{eventSlug}/registrations/{registrationId}/ticket-email/resend", "post")
            .ShouldBe(["ApiKey"]);
    }

    [TestMethod]
    public async Task OpenApiSpec_EmailVerificationEndpointsRequireApiKeyAndVerificationBearer()
    {
        // Act
        using var doc = await GetOpenApiDocumentAsync();

        // Assert
        var root = doc.RootElement;
        var securitySchemes = root.GetProperty("components").GetProperty("securitySchemes");
        securitySchemes.GetProperty("EmailVerificationBearer").GetProperty("scheme").GetString().ShouldBe("bearer");

        GetSecuritySchemeNames(root, "/api/events/{eventSlug}/registrations", "post")
            .ShouldBe(["ApiKey", "EmailVerificationBearer"], ignoreOrder: true);
        GetSecuritySchemeNames(root, "/api/events/{eventSlug}/waitlist/{ticketTypeId}", "post")
            .ShouldBe(["ApiKey", "EmailVerificationBearer"], ignoreOrder: true);
    }

    [TestMethod]
    public async Task OpenApiSpec_AdminEndpointsRequireBearer()
    {
        // Act
        using var doc = await GetOpenApiDocumentAsync();

        // Assert
        GetSecuritySchemeNames(doc.RootElement, "/admin/teams/{teamId}/events", "get")
            .ShouldBe(["Bearer"]);
    }

    private async Task<JsonDocument> GetOpenApiDocumentAsync()
    {
        var response = await Environment.AnonymousApiClient.GetAsync(
            "/openapi/v1.json",
            testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var json = await response.Content.ReadAsStringAsync(testContext.CancellationToken);
        return JsonDocument.Parse(json);
    }

    private static string[] GetSecuritySchemeNames(JsonElement root, string path, string method)
    {
        var securityRequirement = root
            .GetProperty("paths")
            .GetProperty(path)
            .GetProperty(method)
            .GetProperty("security")
            .EnumerateArray()
            .ShouldHaveSingleItem();

        return securityRequirement
            .EnumerateObject()
            .Select(property => property.Name)
            .ToArray();
    }
}
