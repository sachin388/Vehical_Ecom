using System.Diagnostics;
using OpenTelemetry.Trace;

namespace VehicleEcom.Observability;

/// <summary>
/// Helper to instrument distributed cache operations (Redis / In-Memory) with OpenTelemetry spans
/// so that cache hits, misses, sets, and locks appear as child spans in Kibana APM traces.
/// </summary>
public static class CacheTraceHelper
{
    /// <summary>
    /// Starts an OpenTelemetry span for a cache operation.
    /// </summary>
    public static Activity? StartCacheSpan(string operation, string cacheKey, string cacheProvider = "redis")
    {
        var activity = ObservabilityActivitySources.Cache.StartActivity(
            $"Cache {operation} {cacheKey}",
            ActivityKind.Client);

        if (activity != null)
        {
            activity.SetTag("db.system", cacheProvider);
            activity.SetTag("db.operation", operation);
            activity.SetTag("cache.key", cacheKey);
            activity.SetTag("db.redis.key", cacheKey);
        }

        return activity;
    }

    /// <summary>
    /// Executes a cache retrieval function, automatically tracking latency and recording whether it resulted in a cache HIT or MISS.
    /// </summary>
    public static async Task<T?> TraceGetAsync<T>(
        string cacheKey,
        Func<Task<T?>> getFunc,
        string cacheProvider = "redis")
    {
        using var activity = StartCacheSpan("GET", cacheKey, cacheProvider);
        try
        {
            var result = await getFunc();
            var isHit = result != null;
            activity?.SetTag("cache.hit", isHit);
            activity?.SetTag("cache.result", isHit ? "HIT" : "MISS");
            return result;
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.AddException(ex);
            throw;
        }
    }

    /// <summary>
    /// Executes a cache set operation, recording duration and success status.
    /// </summary>
    public static async Task TraceSetAsync(
        string cacheKey,
        Func<Task> setFunc,
        TimeSpan? ttl = null,
        string cacheProvider = "redis")
    {
        using var activity = StartCacheSpan("SET", cacheKey, cacheProvider);
        if (ttl.HasValue)
        {
            activity?.SetTag("cache.ttl_seconds", ttl.Value.TotalSeconds);
        }

        try
        {
            await setFunc();
            activity?.SetStatus(ActivityStatusCode.Ok);
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.AddException(ex);
            throw;
        }
    }

    /// <summary>
    /// Executes a cache invalidation / deletion, recording duration and success status.
    /// </summary>
    public static async Task TraceRemoveAsync(
        string cacheKey,
        Func<Task> removeFunc,
        string cacheProvider = "redis")
    {
        using var activity = StartCacheSpan("DELETE", cacheKey, cacheProvider);
        try
        {
            await removeFunc();
            activity?.SetStatus(ActivityStatusCode.Ok);
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.AddException(ex);
            throw;
        }
    }
}
