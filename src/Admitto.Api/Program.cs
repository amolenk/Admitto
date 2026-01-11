using System.Text.Json;
using System.Text.Json.Serialization;
using Amolenk.Admitto.ApiService.Endpoints;
using Amolenk.Admitto.ApiService.Middleware;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Add default services.
builder.AddServiceDefaults();

// Add application services.
builder.Services
    .AddApplicationCommandHandlers()
    .AddApplicationTransactionalDomainEventHandlers();
    
// Add auth services.
builder
    .AddApiAuthentication()
    .AddApiAuthorization();

// Add validation and error handling middleware.
builder.Services
    .AddApplicationValidationServices()
    .AddProblemDetails()
    .AddExceptionHandler<DomainRuleExceptionHandler>()
    .AddExceptionHandler<ApplicationRuleExceptionHandler>()
    .AddExceptionHandler<GlobalExceptionHandler>();

// Add OpenAPI services.
builder.Services.AddApiOpenApiServices();

// Configure JSON serialization options.
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
});

// Configure CORS to allow all origins, methods, and headers.
// TODO Can be removed once API keys are in place.
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

app.MapDefaultEndpoints();
app.MapAttendeeEndpoints();
app.MapBulkEmailEndpoints();
app.MapContributorEndpoints();
app.MapEmailEndpoints();
app.MapEmailRecipientListEndpoints();
app.MapEmailTemplateEndpoints();
app.MapPublicEndpoints();
app.MapTeamEndpoints();
app.MapTicketedEventEndpoints();

var logger = app.Services.GetRequiredService<ILogger<AppDomain>>();
logger.LogInformation("Starting application...");

app.Run();