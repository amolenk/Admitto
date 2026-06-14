using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Amolenk.Admitto.Api.Auth;
using Amolenk.Admitto.Api.Configuration;
using Amolenk.Admitto.Api.Endpoints;
using Amolenk.Admitto.ApiService.Middleware;
using Amolenk.Admitto.ApiService.OpenApi;
using Amolenk.Admitto.Core.Shared.Application.Auth;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Add default services.
builder.AddServiceDefaults();

// Add modules (application + infrastructure).
builder
    .AddOrganizationModule()
    .AddEmailModule()
    .AddRegistrationsModule()
    .AddBadgesModule();

// Add shared services.
builder.AddSharedServices();

// Add auth services.
builder
    .AddApiAuthentication()
    .AddApiAuthorization();

// Add rate limiting.
var publicRateLimiting = builder.Configuration
    .GetSection(PublicRateLimitingOptions.SectionName)
    .Get<PublicRateLimitingOptions>() ?? new PublicRateLimitingOptions();

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    // Public traffic is expected to be proxied through the event website, so many
    // attendees can legitimately share one source IP. Business-specific throttles
    // such as OTP request limits remain enforced in the application layer.
    options.AddPolicy("public-strict", httpContext =>
        RateLimitPartition.GetSlidingWindowLimiter(
            httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => CreateSlidingWindowOptions(publicRateLimiting.Strict)));

    options.AddPolicy("public-standard", httpContext =>
        RateLimitPartition.GetSlidingWindowLimiter(
            httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => CreateSlidingWindowOptions(publicRateLimiting.Standard)));
});

static SlidingWindowRateLimiterOptions CreateSlidingWindowOptions(RateLimitPolicyOptions policyOptions)
{
    ArgumentOutOfRangeException.ThrowIfNegativeOrZero(policyOptions.PermitLimit);
    ArgumentOutOfRangeException.ThrowIfNegativeOrZero(policyOptions.WindowSeconds);
    ArgumentOutOfRangeException.ThrowIfNegativeOrZero(policyOptions.SegmentsPerWindow);
    ArgumentOutOfRangeException.ThrowIfNegative(policyOptions.QueueLimit);

    return new SlidingWindowRateLimiterOptions
    {
        Window = TimeSpan.FromSeconds(policyOptions.WindowSeconds),
        PermitLimit = policyOptions.PermitLimit,
        SegmentsPerWindow = policyOptions.SegmentsPerWindow,
        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
        QueueLimit = policyOptions.QueueLimit
    };
}

// Add validation and error handling middleware.
builder.Services
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
builder.Services.AddScoped<UserContextResolver>();

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
app.UseMiddleware<UserContextResolutionMiddleware>();
app.UseAuthorization();

app.UseExceptionHandler();
app.UseRateLimiter();

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
