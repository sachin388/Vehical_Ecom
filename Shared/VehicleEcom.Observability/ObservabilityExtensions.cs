using System.Diagnostics;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Exporter;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace VehicleEcom.Observability;

/// <summary>
/// Service Collection and Application Builder extensions for configuring enterprise-grade
/// OpenTelemetry distributed tracing, metrics, and logging with Elastic APM, Elasticsearch, and Kibana.
/// </summary>
public static class ObservabilityExtensions
{
    /// <summary>
    /// Configures OpenTelemetry distributed tracing and metrics with Elastic APM / OTLP exporters.
    /// </summary>
    public static IServiceCollection AddPlatformObservability(
        this IServiceCollection services,
        IConfiguration configuration,
        string defaultServiceName,
        Action<ObservabilityOptions>? configure = null)
    {
        var options = new ObservabilityOptions
        {
            ServiceName = defaultServiceName
        };

        // Bind from appsettings.json if present
        var section = configuration.GetSection(ObservabilityOptions.SectionName);
        if (section.Exists())
        {
            section.Bind(options);
        }

        // Apply caller overrides
        configure?.Invoke(options);

        services.AddSingleton(options);

        // 1. Build standardized OpenTelemetry Resource metadata
        var resourceBuilder = ResourceBuilder.CreateDefault()
            .AddService(
                serviceName: options.ServiceName,
                serviceNamespace: "VehicleEcommerce",
                serviceVersion: options.ServiceVersion,
                serviceInstanceId: $"{Environment.MachineName}-{Environment.ProcessId}")
            .AddAttributes(new[]
            {
                new KeyValuePair<string, object>("deployment.environment", options.Environment),
                new KeyValuePair<string, object>("host.name", Environment.MachineName),
                new KeyValuePair<string, object>("telemetry.sdk.language", "dotnet"),
                new KeyValuePair<string, object>("service.name", options.ServiceName)
            });

        // 2. Register OpenTelemetry Tracing & Metrics
        var otelBuilder = services.AddOpenTelemetry();

        if (options.EnableTracing)
        {
            otelBuilder.WithTracing(tracing =>
            {
                tracing.SetResourceBuilder(resourceBuilder)
                    .AddSource(
                        ObservabilityActivitySources.PlatformNamespace + ".*",
                        ObservabilityActivitySources.Services.Name,
                        ObservabilityActivitySources.Cache.Name,
                        ObservabilityActivitySources.Database.Name,
                        ObservabilityActivitySources.Messaging.Name,
                        ObservabilityActivitySources.Gateway.Name)
                    .AddAspNetCoreInstrumentation(opts =>
                    {
                        opts.RecordException = true;
                        opts.Filter = httpContext =>
                        {
                            var path = httpContext.Request.Path.Value ?? string.Empty;
                            return !path.StartsWith("/health", StringComparison.OrdinalIgnoreCase)
                                && !path.StartsWith("/swagger", StringComparison.OrdinalIgnoreCase)
                                && !path.StartsWith("/favicon.ico", StringComparison.OrdinalIgnoreCase);
                        };
                        opts.EnrichWithHttpRequest = (activity, request) =>
                        {
                            activity.SetTag("http.client_ip", request.HttpContext.Connection.RemoteIpAddress?.ToString());
                            if (request.Headers.TryGetValue("X-Correlation-Id", out var corrId))
                            {
                                activity.SetTag("correlation.id", corrId.ToString());
                            }
                        };
                        opts.EnrichWithHttpResponse = (activity, response) =>
                        {
                            activity.SetTag("http.response_content_length", response.ContentLength);
                        };
                    })
                    .AddHttpClientInstrumentation(opts =>
                    {
                        opts.RecordException = true;
                        opts.EnrichWithHttpRequestMessage = (activity, request) =>
                        {
                            activity.SetTag("http.request_uri", request.RequestUri?.ToString());
                        };
                        opts.EnrichWithHttpResponseMessage = (activity, response) =>
                        {
                            activity.SetTag("http.response_status_code", (int)response.StatusCode);
                        };
                    })
                    .AddEntityFrameworkCoreInstrumentation(opts =>
                    {
                        opts.SetDbStatementForText = options.CaptureSqlStatements;
                        opts.EnrichWithIDbCommand = (activity, command) =>
                        {
                            activity.SetTag("db.statement_type", command.CommandType.ToString());
                        };
                    })
                    .AddRedisInstrumentation();

                // Export to Elastic APM Server / OpenTelemetry Collector via OTLP
                tracing.AddOtlpExporter(otlp =>
                {
                    otlp.Endpoint = new Uri(options.OtlpEndpoint);
                    otlp.Protocol = string.Equals(options.OtlpProtocol, "Grpc", StringComparison.OrdinalIgnoreCase)
                        ? OtlpExportProtocol.Grpc
                        : OtlpExportProtocol.HttpProtobuf;

                    if (!string.IsNullOrWhiteSpace(options.ElasticApmSecretToken))
                    {
                        otlp.Headers = $"Authorization=Bearer {options.ElasticApmSecretToken}";
                    }
                    else if (!string.IsNullOrWhiteSpace(options.ElasticApiKey))
                    {
                        otlp.Headers = $"Authorization=ApiKey {options.ElasticApiKey}";
                    }
                });

                if (options.EnableConsoleExporter)
                {
                    tracing.AddConsoleExporter();
                }
            });
        }

        if (options.EnableMetrics)
        {
            otelBuilder.WithMetrics(metrics =>
            {
                metrics.SetResourceBuilder(resourceBuilder)
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddRuntimeInstrumentation()
                    .AddOtlpExporter(otlp =>
                    {
                        otlp.Endpoint = new Uri(options.OtlpEndpoint);
                        otlp.Protocol = string.Equals(options.OtlpProtocol, "Grpc", StringComparison.OrdinalIgnoreCase)
                            ? OtlpExportProtocol.Grpc
                            : OtlpExportProtocol.HttpProtobuf;

                        if (!string.IsNullOrWhiteSpace(options.ElasticApmSecretToken))
                        {
                            otlp.Headers = $"Authorization=Bearer {options.ElasticApmSecretToken}";
                        }
                        else if (!string.IsNullOrWhiteSpace(options.ElasticApiKey))
                        {
                            otlp.Headers = $"Authorization=ApiKey {options.ElasticApiKey}";
                        }
                    });

                if (options.EnableConsoleExporter)
                {
                    metrics.AddConsoleExporter();
                }
            });
        }

        return services;
    }

    /// <summary>
    /// Configures OpenTelemetry structured logging to export logs to Elastic APM / Elasticsearch.
    /// </summary>
    public static ILoggingBuilder AddPlatformOpenTelemetryLogging(
        this ILoggingBuilder logging,
        IConfiguration configuration,
        string defaultServiceName)
    {
        var options = new ObservabilityOptions
        {
            ServiceName = defaultServiceName
        };

        var section = configuration.GetSection(ObservabilityOptions.SectionName);
        if (section.Exists())
        {
            section.Bind(options);
        }

        if (!options.EnableLogging)
        {
            return logging;
        }

        var resourceBuilder = ResourceBuilder.CreateDefault()
            .AddService(
                serviceName: options.ServiceName,
                serviceNamespace: "VehicleEcommerce",
                serviceVersion: options.ServiceVersion,
                serviceInstanceId: $"{Environment.MachineName}-{Environment.ProcessId}")
            .AddAttributes(new[]
            {
                new KeyValuePair<string, object>("deployment.environment", options.Environment),
                new KeyValuePair<string, object>("service.name", options.ServiceName)
            });

        logging.AddOpenTelemetry(otelLogging =>
        {
            otelLogging.SetResourceBuilder(resourceBuilder);
            otelLogging.IncludeFormattedMessage = true;
            otelLogging.IncludeScopes = true;
            otelLogging.ParseStateValues = true;

            otelLogging.AddOtlpExporter(otlp =>
            {
                otlp.Endpoint = new Uri(options.OtlpEndpoint);
                otlp.Protocol = string.Equals(options.OtlpProtocol, "Grpc", StringComparison.OrdinalIgnoreCase)
                    ? OtlpExportProtocol.Grpc
                    : OtlpExportProtocol.HttpProtobuf;

                if (!string.IsNullOrWhiteSpace(options.ElasticApmSecretToken))
                {
                    otlp.Headers = $"Authorization=Bearer {options.ElasticApmSecretToken}";
                }
                else if (!string.IsNullOrWhiteSpace(options.ElasticApiKey))
                {
                    otlp.Headers = $"Authorization=ApiKey {options.ElasticApiKey}";
                }
            });
        });

        return logging;
    }

    /// <summary>
    /// Adds Observability middleware into the HTTP pipeline to correlate traces and baggage.
    /// </summary>
    public static IApplicationBuilder UsePlatformObservability(this IApplicationBuilder app)
    {
        return app.UseMiddleware<ObservabilityMiddleware>();
    }
}
