using Amolenk.Admitto.ApiService.Endpoints;
using Amolenk.Admitto.ApiService.Handlers;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire client integrations.
builder.AddServiceDefaults();

// Add services to the container.
builder.Services.AddProblemDetails();

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddApplicationServices();
builder.AddInfrastructureServices();

var app = builder.Build();

// Add exception handling middleware
app.UseExceptionHandler(new ExceptionHandlerOptions
{
    ExceptionHandler = new CustomExceptionHandler().HandleAsync
});

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapDefaultEndpoints();
app.MapAttendeeRegistrationEndpoints();
app.MapTicketedEventEndpoints();

app.Run();

