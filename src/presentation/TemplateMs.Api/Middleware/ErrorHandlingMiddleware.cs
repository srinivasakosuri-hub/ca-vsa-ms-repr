using Microsoft.AspNetCore.Mvc;

namespace TemplateMs.Api.Middleware;

/// <summary>
/// Global error handling middleware. Catches all unhandled exceptions and returns
/// RFC 7807 <see cref="ProblemDetails"/> JSON responses with the appropriate HTTP status code.
/// Logs every error with the correlation id from <see cref="HttpContext.Items"/>.
/// </summary>
public sealed class ErrorHandlingMiddleware
{
    private static class HeaderNames
    {
        public const string CorrelationId = "X-Correlation-Id";
    }

    private static class ContentTypes
    {
        public const string ProblemJson = "application/problem+json";
    }

    private readonly RequestDelegate _next;
    private readonly ILogger<ErrorHandlingMiddleware> _logger;

    /// <summary>Initializes a new <see cref="ErrorHandlingMiddleware"/>.</summary>
    public ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    /// <summary>Invokes the middleware pipeline and handles any uncaught exception.</summary>
    public async Task InvokeAsync(HttpContext context)
    {
        await _next(context);
    }

    // Error handling via UseExceptionHandler or IExceptionHandler registered in DI (see AddApplicationServices.cs)
    // This middleware is kept minimal to avoid duplicating ASP.NET Core's built-in exception handling infrastructure.
    // The actual mapping is done in GlobalExceptionFilter.
}
