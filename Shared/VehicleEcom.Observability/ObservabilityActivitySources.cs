using System.Diagnostics;

namespace VehicleEcom.Observability;

/// <summary>
/// Centralized ActivitySource definitions for custom distributed trace spans across the platform.
/// </summary>
public static class ObservabilityActivitySources
{
    public const string PlatformNamespace = "VehicleEcom";

    /// <summary>
    /// ActivitySource for custom service-level spans and transactions.
    /// </summary>
    public static readonly ActivitySource Services = new("VehicleEcom.Services", "1.0.0");

    /// <summary>
    /// ActivitySource for distributed caching and in-memory operations (Redis, MemoryCache).
    /// </summary>
    public static readonly ActivitySource Cache = new("VehicleEcom.Cache", "1.0.0");

    /// <summary>
    /// ActivitySource for custom database / persistence operations.
    /// </summary>
    public static readonly ActivitySource Database = new("VehicleEcom.Database", "1.0.0");

    /// <summary>
    /// ActivitySource for messaging and event streaming (Kafka, EventHubs).
    /// </summary>
    public static readonly ActivitySource Messaging = new("VehicleEcom.Messaging", "1.0.0");

    /// <summary>
    /// ActivitySource for API Gateway routing and proxy relays.
    /// </summary>
    public static readonly ActivitySource Gateway = new("VehicleEcom.Gateway", "1.0.0");
}
