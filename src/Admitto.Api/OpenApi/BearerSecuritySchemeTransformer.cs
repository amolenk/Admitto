using System.Text.Json;
using Amolenk.Admitto.Api.Auth;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace Amolenk.Admitto.ApiService.OpenApi;

/// <summary>
/// Populates OpenAPI security schemes. OAuth endpoints are discovered from the configured
/// <c>Authentication:Bearer:Authority</c> when available.
/// </summary>
internal sealed class BearerSecuritySchemeTransformer(
    IAuthenticationSchemeProvider authenticationSchemeProvider,
    IConfiguration configuration,
    IHttpClientFactory httpClientFactory) : IOpenApiDocumentTransformer
{
    public async Task TransformAsync(
        OpenApiDocument document,
        OpenApiDocumentTransformerContext context,
        CancellationToken cancellationToken)
    {
        var authenticationSchemes = await authenticationSchemeProvider.GetAllSchemesAsync();
        if (!authenticationSchemes.Any(s => s.Name == JwtBearerDefaults.AuthenticationScheme))
            return;

        string? authorizationEndpoint = null;
        string? tokenEndpoint = null;

        var authority = configuration["Authentication:Bearer:Authority"];
        if (!string.IsNullOrEmpty(authority))
        {
            try
            {
                var discoveryUrl = authority.TrimEnd('/') + "/.well-known/openid-configuration";
                var httpClient = httpClientFactory.CreateClient();
                var json = await httpClient.GetStringAsync(discoveryUrl, cancellationToken);
                using var doc = JsonDocument.Parse(json);
                authorizationEndpoint = doc.RootElement.TryGetProperty("authorization_endpoint", out var ae)
                    ? ae.GetString()
                    : null;
                tokenEndpoint = doc.RootElement.TryGetProperty("token_endpoint", out var te)
                    ? te.GetString()
                    : null;
            }
            catch
            {
                // Discovery is best-effort; fall back to no endpoints (still adds the security scheme).
            }
        }

        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();

        document.Components.SecuritySchemes[ApiKeyAuthenticationHandler.SchemeName] = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.ApiKey,
            Name = "X-Api-Key",
            In = ParameterLocation.Header,
            Description = "Public API key for the owning team."
        };

        document.Components.SecuritySchemes[JwtBearerDefaults.AuthenticationScheme] = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.OAuth2,
            Flows = new OpenApiOAuthFlows
            {
                AuthorizationCode = new OpenApiOAuthFlow
                {
                    AuthorizationUrl = authorizationEndpoint is not null ? new Uri(authorizationEndpoint) : null,
                    TokenUrl = tokenEndpoint is not null ? new Uri(tokenEndpoint) : null,
                    Scopes = new Dictionary<string, string>
                    {
                        { "openid", "OpenID Connect" },
                        { "profile", "User profile" },
                        { "email", "Email address" }
                    }
                }
            }
        };

        document.Components.SecuritySchemes[EndpointSecurityRequirementTransformer.EmailVerificationSchemeName] =
            new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "email-verification-token",
                Description = "Email-verification token returned by the public OTP verification endpoint."
            };

        document.SetReferenceHostDocument();
    }
}
