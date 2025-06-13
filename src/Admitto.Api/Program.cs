using System.Text.Json.Serialization;
using Amolenk.Admitto.ApiService.Endpoints;
using Amolenk.Admitto.ApiService.Middleware;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire client integrations.
builder.AddServiceDefaults();

// Add services to the container.
builder.Services.AddProblemDetails();

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.ConfigureHttpJsonOptions(options =>
{
    var converter = new JsonStringEnumConverter();
    options.SerializerOptions.Converters.Add(converter);
});

builder.Services.AddExceptionHandler<DomainExceptionHandler>();
builder.Services.AddExceptionHandler<ValidationExceptionHandler>();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

// TODO
builder.Services.AddDefaultApplicationServices();
builder.AddDefaultInfrastructureServices();

builder.AddDefaultAuthentication();
builder.AddDefaultAuthorization();

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapDefaultEndpoints();
app.MapAttendeeRegistrationEndpoints();
app.MapTeamEndpoints();
app.MapTicketedEventEndpoints();

var logger = app.Services.GetRequiredService<ILogger<AppDomain>>();
logger.LogInformation("Starting application...");

app.Run();
