using System.Text.Json;
using System.Text.Json.Serialization;
using Amolenk.Admitto.ApiService.Endpoints;
using Amolenk.Admitto.ApiService.Handlers;
using Amolenk.Admitto.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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

// Temporary workaround to ensure the database is created and updated
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    dbContext.Database.Migrate(); // Ensures the database is created and updated
}

app.Run();
