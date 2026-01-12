using Amolenk.Admitto.ApiService.Auth;
using Amolenk.Admitto.ApiService.OpenApi;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

public static class DependencyInjection
{
    extension(IServiceCollection services)
    {
        public void AddApiOpenApiServices()
        {
            // Add OpenAPI/Swagger generation with Bearer token security scheme.
            services.AddOpenApi(options =>
            {
                // Add Bearer token security scheme to the OpenAPI output.
                options.AddDocumentTransformer<BearerSecuritySchemeTransformer>();
                
                options.AddSchemaTransformer<NumberTypeTransformer>();
                NumberTypeTransformer.MapType<TimeSpan>(
                    new OpenApiSchema { Type = JsonSchemaType.String, Format = "duration" });

            });
        }
    }

    extension<TBuilder>(TBuilder builder) where TBuilder : IHostApplicationBuilder
    {
        public TBuilder AddApiAuthentication()
        {
            builder.Services
                .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    builder.Configuration.Bind("Authentication:Bearer", options);

                    options.IncludeErrorDetails = builder.Environment.IsDevelopment();
                    options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
                    options.TokenValidationParameters.ValidateAudience = true;
                    options.TokenValidationParameters.ValidateIssuer = true;

                    options.Events = new JwtBearerEvents
                    {
                        OnChallenge = context =>
                        {
                            context.HandleResponse();
                            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                            context.Response.ContentType = "application/problem+json";

                            var problem = new ProblemDetails
                            {
                                Status = StatusCodes.Status401Unauthorized,
                                Title = "Unauthorized",
                                Detail = "You are not authorized to access this resource."
                            };

                            return context.Response.WriteAsJsonAsync(problem);
                        },
                        OnForbidden = context =>
                        {
                            context.Response.StatusCode = StatusCodes.Status403Forbidden;
                            context.Response.ContentType = "application/problem+json";

                            var problem = new ProblemDetails
                            {
                                Status = StatusCodes.Status403Forbidden,
                                Title = "Forbidden",
                                Detail = "You do not have permission to access this resource."
                            };

                            return context.Response.WriteAsJsonAsync(problem);
                        }
                    };
                });

            builder.AddInfrastructureUserManagementServices();

            return builder;
        }

        public TBuilder AddApiAuthorization()
        {
            builder.Services
                .AddApplicationAuthorizationServices()
                .AddScoped<IAuthorizationHandler, AdminAuthorizationHandler>()
                .AddScoped<IAuthorizationHandler, TeamMemberRoleAuthorizationHandler>()
                .AddAuthorization();

            return builder;
        }
    }
}