using System.Security.Claims;
using Amolenk.Admitto.Api.Auth;
using Microsoft.AspNetCore.Http;
using Shouldly;

namespace Amolenk.Admitto.Api.Tests.Auth;

[TestClass]
public sealed class UserContextResolutionMiddlewareTests
{
    // Given a JWT-authenticated request whose teamId route value is not a valid GUID
    // When the user context resolution middleware runs
    // Then it responds with 403 Forbidden and does not call the next middleware
    [TestMethod]
    public async Task InvokeAsync_JwtRequestWithInvalidTeamId_Returns403Forbidden()
    {
        var nextCalled = false;
        var sut = new UserContextResolutionMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });
        var context = CreateJwtContext();
        context.Request.RouteValues["teamId"] = "not-a-guid";

        await sut.InvokeAsync(context, null!);

        context.Response.StatusCode.ShouldBe(StatusCodes.Status403Forbidden);
        nextCalled.ShouldBeFalse();
    }

    // Given a JWT-authenticated request with an eventId route value but no teamId
    // When the user context resolution middleware runs
    // Then it responds with 403 Forbidden and does not call the next middleware
    [TestMethod]
    public async Task InvokeAsync_JwtRequestWithEventIdWithoutTeamId_Returns403Forbidden()
    {
        var nextCalled = false;
        var sut = new UserContextResolutionMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });
        var context = CreateJwtContext();
        context.Request.RouteValues["eventId"] = Guid.NewGuid().ToString();

        await sut.InvokeAsync(context, null!);

        context.Response.StatusCode.ShouldBe(StatusCodes.Status403Forbidden);
        nextCalled.ShouldBeFalse();
    }

    // Given an API-key-authenticated request carrying admin route values like eventId
    // When the user context resolution middleware runs
    // Then it skips resolution and calls the next middleware
    [TestMethod]
    public async Task InvokeAsync_ApiKeyRequestWithAdminRouteValues_SkipsResolution()
    {
        var nextCalled = false;
        var sut = new UserContextResolutionMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });
        var context = CreateApiKeyContext();
        context.Request.RouteValues["eventId"] = Guid.NewGuid().ToString();

        await sut.InvokeAsync(context, null!);

        nextCalled.ShouldBeTrue();
        context.Response.StatusCode.ShouldBe(StatusCodes.Status200OK);
    }

    private static DefaultHttpContext CreateJwtContext()
        => new()
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(
            [
                new Claim(ClaimTypes.NameIdentifier, "test-sub"),
                new Claim(ClaimTypes.Email, "alice@example.com")
            ],
            "Bearer"))
        };

    private static DefaultHttpContext CreateApiKeyContext()
        => new()
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(
            [
                new Claim(ClaimTypes.NameIdentifier, "api-key")
            ],
            ApiKeyAuthenticationHandler.SchemeName))
        };
}
