using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Http;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using OpenTelemetry.Resources;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Logs;
using Azure.Monitor.OpenTelemetry.Exporter;

namespace SpeechAnalytics.ServiceDefaults;

public static class ServiceDefaultsExtensions
{
    public static IHostApplicationBuilder AddAppServiceDefaults(this IHostApplicationBuilder builder)
    {
        var serviceName = Assembly.GetEntryAssembly()?.GetName().Name ?? "service";
        string? aiConnString = builder.Configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"]
            ?? builder.Configuration["APPINSIGHTS_CONNECTIONSTRING"];

        builder.Services.AddOpenTelemetry()
            .ConfigureResource(rb => rb.AddService(serviceName))
            .WithTracing(tracing =>
            {
                tracing
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddOtlpExporter();
                if (!string.IsNullOrWhiteSpace(aiConnString))
                {
                    tracing.AddAzureMonitorTraceExporter(o => o.ConnectionString = aiConnString);
                }
            })
            .WithMetrics(metrics =>
            {
                metrics
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddRuntimeInstrumentation()
                    .AddOtlpExporter();
                if (!string.IsNullOrWhiteSpace(aiConnString))
                {
                    metrics.AddAzureMonitorMetricExporter(o => o.ConnectionString = aiConnString);
                }
            });

        builder.Services.AddLogging(lb =>
        {
            lb.AddOpenTelemetry(o =>
            {
                o.IncludeScopes = true;
                o.ParseStateValues = true;
                o.IncludeFormattedMessage = true;
                o.AddOtlpExporter();
                if (!string.IsNullOrWhiteSpace(aiConnString))
                {
                    o.AddAzureMonitorLogExporter(opt => opt.ConnectionString = aiConnString);
                }
            });
        });

        builder.Services.ConfigureHttpClientDefaults(http =>
        {
            http.AddStandardResilienceHandler();
        });

        builder.Services.AddHealthChecks();
        return builder;
    }

    public static WebApplication MapAppDefaultEndpoints(this WebApplication app)
    {
        app.MapHealthChecks("/health");
        return app;
    }
}
