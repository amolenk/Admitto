using Amolenk.Admitto.Api.Auth;
using Amolenk.Admitto.Core.Shared.Application.Http;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace Amolenk.Admitto.ApiService.OpenApi;

internal sealed class EndpointSecurityRequirementTransformer : IOpenApiOperationTransformer
{
    internal const string EmailVerificationSchemeName = "EmailVerificationBearer";

    public Task TransformAsync(
        OpenApiOperation operation,
        OpenApiOperationTransformerContext context,
        CancellationToken cancellationToken)
    {
        var metadata = context.Description.ActionDescriptor.EndpointMetadata;

        if (HasAuthenticationScheme(metadata, ApiKeyAuthenticationHandler.SchemeName))
        {
            operation.Security ??= [];
            var requirement = new OpenApiSecurityRequirement
            {
                [new OpenApiSecuritySchemeReference(ApiKeyAuthenticationHandler.SchemeName)] = []
            };

            if (metadata.OfType<EmailVerificationBearerTokenRequiredMetadata>().Any())
                requirement[new OpenApiSecuritySchemeReference(EmailVerificationSchemeName)] = [];

            operation.Security.Add(requirement);
        }
        else if (metadata.OfType<IAuthorizeData>().Any() || metadata.OfType<AuthorizationPolicy>().Any())
        {
            operation.Security ??= [];
            operation.Security.Add(new OpenApiSecurityRequirement
            {
                [new OpenApiSecuritySchemeReference(JwtBearerDefaults.AuthenticationScheme)] = []
            });
        }

        return Task.CompletedTask;
    }

    private static bool HasAuthenticationScheme(IList<object> metadata, string schemeName)
    {
        return metadata.OfType<IAuthorizeData>().Any(data => ContainsScheme(data.AuthenticationSchemes, schemeName))
            || metadata.OfType<AuthorizationPolicy>().Any(policy => policy.AuthenticationSchemes.Contains(schemeName));
    }

    private static bool ContainsScheme(string? authenticationSchemes, string schemeName)
    {
        return authenticationSchemes?
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Contains(schemeName, StringComparer.Ordinal) == true;
    }
}
