using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace Amolenk.Admitto.ApiService.OpenApi;

internal sealed class BearerSecuritySchemeTransformer : IOpenApiDocumentTransformer
{
    public Task TransformAsync(
        OpenApiDocument document,
        OpenApiDocumentTransformerContext context,
        CancellationToken cancellationToken)
    {
        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();

        var configuration = context.ApplicationServices.GetRequiredService<IConfiguration>();
        var authority = configuration["Authentication:Bearer:Authority"];
        
        // Add OAuth2 security scheme (Authorization Code flow only)
        document.Components.SecuritySchemes.Add("oauth2", new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.OAuth2,
            Flows = new OpenApiOAuthFlows
            {
                AuthorizationCode = new OpenApiOAuthFlow
                {
                    // These URLs are specific to Keycloak's OpenID Connect implementation, but that's okay because
                    // we only enable OpenAPI for local development.
                    AuthorizationUrl = new Uri($"{authority}/protocol/openid-connect/auth"),
                    TokenUrl = new Uri($"{authority}/protocol/openid-connect/token"),
                    Scopes = new Dictionary<string, string>
                    {
                        { "manage", "Access the Management API" },
                        { "openid", "Access the OpenID Connect user profile" },
                        { "email", "Access the user's email address" },
                        { "profile", "Access the user's profile" }
                    }
                }
            }
        });
        
        // Apply security requirement globally
        // TODO Not all endpoints require authorization at the moment
        document.Security = [
            new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecuritySchemeReference("oauth2"),
                    ["manage", "profile", "email", "openid"]
                }
            }
        ];
        
        // Set the host document for all elements
        // including the security scheme references
        document.SetReferenceHostDocument();

        return Task.CompletedTask;
    }
}