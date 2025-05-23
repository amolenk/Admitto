using System.Text.Json;
using System.Text.Json.Serialization;
using Amolenk.Admitto.ApiService.Endpoints;
using Amolenk.Admitto.ApiService.Middleware;
using Amolenk.Admitto.Infrastructure.Persistence;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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

builder.Services.AddExceptionHandler<DbExceptionHandler>();
builder.Services.AddExceptionHandler<DomainExceptionHandler>();
builder.Services.AddExceptionHandler<ValidationExceptionHandler>();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

// TODO
builder.Services.AddApplicationServices();
builder.AddInfrastructureServices();

var app = builder.Build();
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapDefaultEndpoints();
app.MapAttendeeRegistrationEndpoints();
app.MapTeamEndpoints();
app.MapTicketedEventEndpoints();

app.Run();
