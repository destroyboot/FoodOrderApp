using System.Diagnostics;
using System.Globalization;

namespace API.Middleware;

public sealed class RequestTimingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestTimingMiddleware> _logger;
    private readonly IConfiguration _configuration;

    public RequestTimingMiddleware(
        RequestDelegate next,
        ILogger<RequestTimingMiddleware> logger,
        IConfiguration configuration)
    {
        _next = next;
        _logger = logger;
        _configuration = configuration;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.Request.Path.StartsWithSegments("/hubs"))
        {
            await _next(context);
            return;
        }

        var stopwatch = Stopwatch.StartNew();

        context.Response.OnStarting(() =>
        {
            context.Response.Headers["X-Elapsed-Ms"] = stopwatch.ElapsedMilliseconds.ToString(CultureInfo.InvariantCulture);
            return Task.CompletedTask;
        });

        try
        {
            await _next(context);
        }
        finally
        {
            stopwatch.Stop();
            var thresholdMs = _configuration.GetValue<int?>("Diagnostics:SlowRequestThresholdMs") ?? 500;

            if (stopwatch.ElapsedMilliseconds >= thresholdMs)
            {
                _logger.LogWarning(
                    "Slow request {Method} {Path}{QueryString} responded {StatusCode} in {ElapsedMilliseconds} ms.",
                    context.Request.Method,
                    context.Request.Path,
                    context.Request.QueryString,
                    context.Response.StatusCode,
                    stopwatch.ElapsedMilliseconds);
            }
        }
    }
}
