using System.Text.Json;
using System.Text.Json.Serialization;
using Amolenk.Admitto.ApiService.Auth;
using Amolenk.Admitto.ApiService.Endpoints;
using Amolenk.Admitto.ApiService.Middleware;
using Amolenk.Admitto.ApiService.OpenApi;
using Amolenk.Admitto.Organization.Application;
using Amolenk.Admitto.Registrations.Application;
using Amolenk.Admitto.Registrations.Infrastructure;
using Amolenk.Admitto.Shared.Application.Auth;
using Amolenk.Admitto.Shared.Application.Http;
using Amolenk.Admitto.Shared.Infrastructure;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Add default services.
builder.AddServiceDefaults();

// Add application services.
builder
    .AddOrganizationApplicationServices()
    .Services
    .AddMessagingApplicationServices()
    .AddValidationApplicationServices()
    .AddRegistrationsApplicationServices();

// Add infrastructure services.
builder
    .AddSharedInfrastructureMessagingServices()
    .AddOrganizationInfrastructureServices()
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

var logger = app.Services.GetRequiredService<ILogger<AppDomain>>();
logger.LogInformation("Starting application...");

app.Run();
