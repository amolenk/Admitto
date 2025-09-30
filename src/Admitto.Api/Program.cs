using System.Text.Json;
using System.Text.Json.Serialization;
using Amolenk.Admitto.ApiService.Endpoints;
using Amolenk.Admitto.ApiService.Middleware;
using Amolenk.Admitto.Application.Common.Abstractions;
using FluentValidation;
using FluentValidation.Internal;
using Humanizer;
using Microsoft.AspNetCore.Authorization;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire client integrations.
builder.AddServiceDefaults();

// Add services to the container.
builder.Services.AddProblemDetails();

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
});

builder.Services.AddExceptionHandler<DomainRuleExceptionHandler>();
builder.Services.AddExceptionHandler<ApplicationRuleExceptionHandler>();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

// TODO
builder.Services.AddDefaultApplicationServices();
builder.Services.AddEmailServices();
builder.Services.AddCommandHandlers();

builder.Services.AddScoped<IAuthorizationHandler, AuthorizationHandler>();

builder.AddDefaultInfrastructureServices();

builder.AddDefaultAuthentication();
builder.AddDefaultAuthorization();

builder.Services.AddCors(options =>
{
    options.AddPolicy(
        "AllowAll",
        policy =>
        {
            // TODO Can we tighten this (headers? methods?)
            policy.AllowAnyOrigin()
                .AllowAnyMethod()
                .AllowAnyHeader();
        });
});

// Use camel case for FluentValidation property names
ValidatorOptions.Global.DisplayNameResolver = (_, member, _) => member?.Name.Humanize();
ValidatorOptions.Global.PropertyNameResolver = (_, memberInfo, expression) =>
{
    if (expression != null)
    {
        var chain = PropertyChain.FromExpression(expression);
        if (chain.Count > 0)
        {
            var propertyNames = chain.ToString().Split(ValidatorOptions.Global.PropertyChainSeparator);
            if (propertyNames.Length == 1)
            {
                return propertyNames[0].Camelize();
            }

            return string.Join(
                ValidatorOptions.Global.PropertyChainSeparator,
                propertyNames.Select(n => n.Camelize()));
        }
    }

    return memberInfo?.Name.Camelize();
};

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    using var migrationScope = app.Services.CreateScope();
    var migrationService = migrationScope.ServiceProvider.GetRequiredService<IMigrationService>();
    await migrationService.MigrateAllAsync();
}

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
app.MapEmailTemplateEndpoints();
app.MapMigrationEndpoints();
app.MapPublicEndpoints();
app.MapTeamEndpoints();
app.MapTicketedEventEndpoints();

var logger = app.Services.GetRequiredService<ILogger<AppDomain>>();
logger.LogInformation("Starting application...");

app.Run();