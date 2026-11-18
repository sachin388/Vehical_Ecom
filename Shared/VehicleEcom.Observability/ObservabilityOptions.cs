namespace VehicleEcom.Observability;

/// <summary>
/// Configuration options for OpenTelemetry, Elastic APM, and Kibana distributed tracing.
/// </summary>
public class ObservabilityOptions
{
    public const string SectionName = "Observability";

    /// <summary>
    /// Logical name of the microservice (e.g. "ApiGateway", "Items.API", "SalesOrder.API").
    /// </summary>
    public string ServiceName { get; set; } = "UnknownService";

    /// <summary>
    /// Version of the microservice.
    /// </summary>
    public string ServiceVersion { get; set; } = "1.0.0";

    /// <summary>
    /// Deployment environment name (e.g. "Development", "Staging", "Production").
    /// </summary>
    public string Environment { get; set; } = "Development";

    /// <summary>
    /// OpenTelemetry OTLP Ingestion Endpoint.
    /// For Elastic APM Server 8.x: "http://localhost:8200" (HTTP/Protobuf) or "http://localhost:4317" (gRPC) / "http://localhost:4318" (HTTP).
    /// </summary>
    public string OtlpEndpoint { get; set; } = "http://localhost:8200";

    /// <summary>
    /// OTLP protocol: "HttpProtobuf" (recommended for Elastic APM :8200 or :4318) or "Grpc" (:4317).
    /// </summary>
    public string OtlpProtocol { get; set; } = "HttpProtobuf";

    /// <summary>
    /// Optional Elastic APM Server Secret Token (if authentication is enabled on Elastic APM).
    /// </summary>
    public string? ElasticApmSecretToken { get; set; }

    /// <summary>
    /// Optional Elastic API Key (e.g. for Elastic Cloud).
    /// </summary>
    public string? ElasticApiKey { get; set; }

    /// <summary>
    /// Enable Distributed Tracing (default: true).
    /// </summary>
    public bool EnableTracing { get; set; } = true;

    /// <summary>
    /// Enable Runtime & Application Metrics (default: true).
    /// </summary>
    public bool EnableMetrics { get; set; } = true;

    /// <summary>
    /// Enable OpenTelemetry Log streaming to Elastic (default: true).
    /// </summary>
    public bool EnableLogging { get; set; } = true;

    /// <summary>
    /// Enable local console exporter for debugging (default: false).
    /// </summary>
    public bool EnableConsoleExporter { get; set; } = false;

    /// <summary>
    /// Capture raw SQL statements in EF Core trace spans.
    /// </summary>
    public bool CaptureSqlStatements { get; set; } = true;
}
