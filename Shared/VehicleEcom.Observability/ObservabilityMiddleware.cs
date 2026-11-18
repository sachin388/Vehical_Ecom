using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace VehicleEcom.Observability;

/// <summary>
/// Middleware that correlates HTTP requests with OpenTelemetry traces, linking X-Correlation-Id
/// with W3C TraceContext (traceparent), and enriching the active span with user and client context.
/// </summary>
public class ObservabilityMiddleware
{
    private const string CorrelationHeader = "X-Correlation-Id";
    private const string TraceIdHeader = "X-Trace-Id";
    private readonly RequestDelegate _next;
    private readonly ILogger<ObservabilityMiddleware> _logger;

    public ObservabilityMiddleware(RequestDelegate next, ILogger<ObservabilityMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // 1. Resolve or generate Correlation ID
        var correlationId = context.Request.Headers[CorrelationHeader].FirstOrDefault();
        var currentActivity = Activity.Current;

        if (string.IsNullOrWhiteSpace(correlationId))
        {
            correlationId = currentActivity?.TraceId.ToString() ?? Guid.NewGuid().ToString("D");
            context.Request.Headers[CorrelationHeader] = correlationId;
        }

        // Attach to ASP.NET trace identifier and response headers
        context.TraceIdentifier = correlationId;
        context.Response.Headers[CorrelationHeader] = correlationId;

        if (currentActivity != null)
        {
            var traceId = currentActivity.TraceId.ToString();
            context.Response.Headers[TraceIdHeader] = traceId;

            // Enrich current activity span tags
            currentActivity.SetTag("correlation.id", correlationId);
            currentActivity.SetTag("http.client_ip", context.Connection.RemoteIpAddress?.ToString() ?? "unknown");

            // Capture authenticated user context if available
            if (context.User.Identity?.IsAuthenticated == true)
            {
                var userId = context.User.FindFirst("sub")?.Value 
                             ?? context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                var userEmail = context.User.FindFirst("email")?.Value 
                               ?? context.User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;

                if (!string.IsNullOrEmpty(userId))
                {
                    currentActivity.SetTag("user.id", userId);
                }
                if (!string.IsNullOrEmpty(userEmail))
                {
                    currentActivity.SetTag("user.email", userEmail);
                }
            }

            // Capture relayed headers from Gateway if present
            if (context.Request.Headers.TryGetValue("X-User-Id", out var relayedUserId))
            {
                currentActivity.SetTag("user.id", relayedUserId.ToString());
            }
            if (context.Request.Headers.TryGetValue("X-User-Email", out var relayedEmail))
            {
                currentActivity.SetTag("user.email", relayedEmail.ToString());
            }
        }

        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            if (currentActivity != null)
            {
                currentActivity.SetStatus(ActivityStatusCode.Error, ex.Message);
                currentActivity.AddException(ex);
            }
            throw;
        }
    }
}
