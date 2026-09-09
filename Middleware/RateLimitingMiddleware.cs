// Middleware/RateLimitingMiddleware.cs
using System.Collections.Concurrent;

namespace cateringflow.Middleware;

public class RateLimitingMiddleware
{
    private readonly RequestDelegate _next;
    private static readonly ConcurrentDictionary<string, List<DateTime>> RequestTimestamps = new();
    private const int MaxRequestsPerMinute = 30;

    public RateLimitingMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value ?? string.Empty;

        // Apply rate limit specifically to login/auth attempts
        if (path.StartsWith("/Account/Login", StringComparison.OrdinalIgnoreCase) && 
            HttpMethods.IsPost(context.Request.Method))
        {
            var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown_client";
            var now = DateTime.UtcNow;

            var timestamps = RequestTimestamps.GetOrAdd(ip, _ => new List<DateTime>());
            bool isRateLimited = false;

            lock (timestamps)
            {
                timestamps.RemoveAll(t => (now - t).TotalMinutes > 1);

                if (timestamps.Count >= MaxRequestsPerMinute)
                {
                    isRateLimited = true;
                }
                else
                {
                    timestamps.Add(now);
                }
            }

            if (isRateLimited)
            {
                context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                context.Response.ContentType = "text/plain";
                await context.Response.WriteAsync("Too many login attempts. Please wait 1 minute before trying again.");
                return;
            }
        }

        await _next(context);
    }
}
