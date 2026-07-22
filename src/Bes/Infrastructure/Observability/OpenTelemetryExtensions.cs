using Bardie.ModuleChannel.Manifest;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace Bes.Infrastructure.Observability;

public static class OpenTelemetryExtensions
{
    /// <summary>
    /// Registers OTLP traces/metrics with <c>service.name</c> from <see cref="ModuleManifest.OtelServiceName"/>.
    /// </summary>
    public static WebApplicationBuilder AddBesOpenTelemetry(
        this WebApplicationBuilder builder,
        ModuleManifest manifest)
    {
        var serviceName = string.IsNullOrWhiteSpace(manifest.OtelServiceName)
            ? "bardie.auth.bes"
            : manifest.OtelServiceName;

        builder.Services.AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService(
                serviceName: serviceName,
                serviceVersion: typeof(OpenTelemetryExtensions).Assembly.GetName().Version?.ToString()))
            .WithTracing(tracing => tracing
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddGrpcClientInstrumentation()
                .AddOtlpExporter())
            .WithMetrics(metrics => metrics
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddOtlpExporter());

        return builder;
    }
}
