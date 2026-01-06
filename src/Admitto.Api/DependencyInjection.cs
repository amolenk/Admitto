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

                // Kiota (which is used for client generation in the CLI) can't handle types of integer|string or
                // number|string. The types are generated this way because System.Text.Json by default allows reading
                // numbers from strings. We could disable this globally by setting
                // JsonSerializerOptions.NumberHandling = JsonNumberHandling.Strict, but reading numbers from string
                // is a useful feature to have in many scenarios, so instead we only adjust the OpenAPI schema here.
                options.AddSchemaTransformer<NumberTypeTransformer>();
                NumberTypeTransformer.MapType<int>(
                    new OpenApiSchema { Type = JsonSchemaType.Integer, Format = "int32" });
                NumberTypeTransformer.MapType<int?>(
                    new OpenApiSchema { Type = JsonSchemaType.Integer | JsonSchemaType.Null, Format = "int32" });
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