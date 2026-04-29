namespace KazoOCR.Api.Middleware;

/// <summary>
/// Middleware that validates API key from X-Api-Key header when KAZO_API_KEY is configured.
/// If KAZO_API_KEY is not set or empty, all requests pass through (open mode).
/// </summary>
public class ApiKeyMiddleware
{
    private const string ApiKeyHeaderName = "X-Api-Key";
    private readonly RequestDelegate _next;
    private readonly string? _apiKey;
    private readonly ILogger<ApiKeyMiddleware> _logger;

    public ApiKeyMiddleware(RequestDelegate next, IConfiguration configuration, ILogger<ApiKeyMiddleware> logger)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        
        _next = next;
        _apiKey = configuration["API_KEY"];
        _logger = logger;

        if (string.IsNullOrEmpty(_apiKey))
        {
            _logger.LogInformation("KAZO_API_KEY not configured - API key authentication disabled (open mode)");
        }
        else
        {
            _logger.LogInformation("API key authentication enabled");
        }
    }

    public async Task InvokeAsync(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        
        // If no API key is configured, allow all requests (open mode)
        if (string.IsNullOrEmpty(_apiKey))
        {
            await _next(context);
            return;
        }

        // Skip API key check for auth endpoints (they handle their own auth)
        var path = context.Request.Path.Value?.ToLowerInvariant() ?? "";
        if (path.StartsWith("/api/auth/", StringComparison.Ordinal))
        {
            await _next(context);
            return;
        }

        // Skip API key check for health endpoint
        if (path == "/health")
        {
            await _next(context);
            return;
        }

        // Skip API key check for OpenAPI/Scalar endpoints
        if (path.StartsWith("/openapi", StringComparison.Ordinal)
            || path.StartsWith("/docs", StringComparison.Ordinal))
        {
            await _next(context);
            return;
        }

        // Check for API key header
        if (!context.Request.Headers.TryGetValue(ApiKeyHeaderName, out var providedApiKey))
        {
            _logger.LogWarning("Request missing {HeaderName} header", ApiKeyHeaderName);
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(new { error = "API key required" });
            return;
        }

        // Validate API key
        if (!string.Equals(_apiKey, providedApiKey.ToString(), StringComparison.Ordinal))
        {
            _logger.LogWarning("Request with invalid API key");
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(new { error = "Invalid API key" });
            return;
        }

        await _next(context);
    }
}

/// <summary>
/// Extension methods for API key middleware
/// </summary>
public static class ApiKeyMiddlewareExtensions
{
    public static IApplicationBuilder UseApiKeyAuthentication(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<ApiKeyMiddleware>();
    }
}
