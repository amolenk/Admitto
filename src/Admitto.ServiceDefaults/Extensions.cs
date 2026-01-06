using Amolenk.Admitto.Application.Common;
using Azure.Monitor.OpenTelemetry.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

namespace Microsoft.Extensions.Hosting;

// Adds common .NET Aspire services: service discovery, resilience, health checks, and OpenTelemetry.
// This project should be referenced by each service project in your solution.
// To learn more about using this project, see https://aka.ms/dotnet/aspire/service-defaults
public static class Extensions
{
    extension<TBuilder>(TBuilder builder) where TBuilder : IHostApplicationBuilder
    {
        public void AddServiceDefaults()
        {
            builder.ConfigureOpenTelemetry();

            builder.AddDefaultHealthChecks();

            builder.Services.AddServiceDiscovery();

            builder.Services.ConfigureHttpClientDefaults(http =>
            {
                // Turn on resilience by default
                http.AddStandardResilienceHandler();

                // Turn on service discovery by default
                http.AddServiceDiscovery();
            });

            builder.AddDataProtection();
                
            // TODO Optimize cache size
            builder.Services.AddMemoryCache();

            builder.Services.AddHttpContextAccessor();

            // Add default infrastructure services.
            builder
                .AddInfrastructureDatabaseServices()
                .AddInfrastructureMessagingServices()
                .AddInfrastructureJobServices();
        
            // Add default application services.
            builder.Services.AddApplicationCryptographyServices();
        }

        private void ConfigureOpenTelemetry()
        {
            builder.Logging.AddOpenTelemetry(logging =>
            {
                logging.IncludeFormattedMessage = true;
                logging.IncludeScopes = true;
            });

            builder.Services.AddOpenTelemetry()
                .WithMetrics(metrics =>
                {
                    metrics.AddAspNetCoreInstrumentation()
                        .AddHttpClientInstrumentation()
                        .AddRuntimeInstrumentation()
                        .AddMeter("Polly");
                })
                .WithTracing(tracing =>
                {
                    tracing.AddSource(builder.Environment.ApplicationName)
                        .AddAspNetCoreInstrumentation()
                        // Uncomment the following line to enable gRPC instrumentation (requires the OpenTelemetry.Instrumentation.GrpcNetClient package)
                        //.AddGrpcClientInstrumentation()
                        .AddHttpClientInstrumentation()
                        .AddQuartzInstrumentation()
                        .AddSource("Polly")
                        .AddSource(AdmittoActivitySource.Name);
                });

            builder.AddOpenTelemetryExporters();
        }

        private void AddOpenTelemetryExporters()
        {
            var useOtlpExporter = !string.IsNullOrWhiteSpace(builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]);

            if (useOtlpExporter)
            {
                builder.Services.AddOpenTelemetry().UseOtlpExporter();
            }

            // Enable Azure Monitor exporter for Application Insights
            if (!string.IsNullOrEmpty(builder.Configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"]))
            {
                builder.Services.AddOpenTelemetry()
                    .UseAzureMonitor(options =>
                    {
                        options.ConnectionString = builder.Configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"];
                        options.SamplingRatio = 0.1f; // Sample 10% of telemetry
                    });
            }
        }

        private void AddDefaultHealthChecks()
        {
            builder.Services.AddHealthChecks()
                // Add a default liveness check to ensure app is responsive
                .AddCheck("self", () => HealthCheckResult.Healthy(), ["live"]);
        }

        private void AddDataProtection()
        {
            builder.Services.AddDataProtection()
                .SetApplicationName("Admitto");
        }
    }

    public static WebApplication MapDefaultEndpoints(this WebApplication app)
    {
        // Adding health checks endpoints to applications in non-development environments has security implications.
        // See https://aka.ms/dotnet/aspire/healthchecks for details before enabling these endpoints in non-development environments.
        if (app.Environment.IsDevelopment())
        {
            // All health checks must pass for app to be considered ready to accept traffic after starting
            app.MapHealthChecks("/health");

            // Only health checks tagged with the "live" tag must pass for app to be considered alive
            app.MapHealthChecks(
                "/alive",
                new HealthCheckOptions
                {
                    Predicate = r => r.Tags.Contains("live")
                });
        }

        return app;
    }
}