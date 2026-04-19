using System.Text.Json;
using System.Text.Json.Serialization;
using Amolenk.Admitto.Api.Endpoints;
using Amolenk.Admitto.ApiService.Auth;
using Amolenk.Admitto.ApiService.Middleware;
using Amolenk.Admitto.ApiService.OpenApi;
using Amolenk.Admitto.Module.Email.Application;
using Amolenk.Admitto.Module.Organization.Application;
using Amolenk.Admitto.Module.Organization.Application.Http;
using Amolenk.Admitto.Module.Registrations.Application;
using Amolenk.Admitto.Module.Registrations.Infrastructure;
using Amolenk.Admitto.Module.Shared.Application.Auth;
using Amolenk.Admitto.Module.Shared.Application.Http;
using Amolenk.Admitto.Module.Shared.Infrastructure;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Add default services.
builder.AddServiceDefaults();

// Add application services.
builder
    .AddOrganizationApplicationServices()
    .AddEmailApplicationServices()
    .Services
    .AddRegistrationsApplicationServices()
    .AddMessagingApplicationServices()
    .AddValidationApplicationServices();

// Add infrastructure services.
builder
    .AddSharedInfrastructureMessagingServices()
    .AddOrganizationInfrastructureServices()
    .AddEmailInfrastructureServices()
    .AddRegistrationsInfrastructureServices()
    .Services
    .AddSharedInfrastructureServices();

// Add auth services.
builder
    .AddApiAuthentication()
    .AddApiAuthorization();

// Add validation and error handling middleware.
builder.Services
    .AddValidationApplicationServices()
    .AddProblemDetails()
    // TODO
    // .AddExceptionHandler<DomainRuleExceptionHandler>()
    .AddExceptionHandler<ApplicationErrorExceptionHandler>()
    .AddExceptionHandler<GlobalExceptionHandler>();

// Add OpenAPI services.
builder.Services.AddApiOpenApiServices();

// Configure JSON serialization options.
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new Iso8601TimeSpanConverter());
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
});

// TODO
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IUserContextAccessor, HttpContextUserContextAccessor>();
builder.Services.AddScoped<IAdministratorRoleService, AdministratorRoleService>();
builder.Services.AddScoped<IOrganizationScopeResolver, OrganizationScopeResolver>();

// Configure CORS to allow all origins, methods, and headers.
// TODO Can be removed once API keys are in place? Or still needed for UI?
builder.Services.AddCors(options =>
{
    options.AddPolicy(
        "AllowAll",
        policy =>
        {
            policy.AllowAnyOrigin()
                .AllowAnyMethod()
                .AllowAnyHeader();
        });
});

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseCors();

app.UseRequestTimeouts();
app.UseOutputCache();

app.MapDefaultEndpoints();
app.MapAdminEndpoints();
app.MapPublicEndpoints();

var logger = app.Services.GetRequiredService<ILogger<AppDomain>>();
logger.LogInformation("Starting application...");

app.Run();
