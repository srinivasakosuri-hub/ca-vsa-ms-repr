

namespace TemplateMs.Api.Middleware;

/// <summary>
/// Middleware that reads or generates a correlation id for every HTTP request.
/// The id is read from <c>X-Correlation-Id</c> request header (or a new <see cref="Guid"/> is created),
/// stored in <see cref="HttpContext.Items"/>, and appended to the response headers.
/// </summary>
public sealed class CorrelationIdMiddleware
{
    private static class Headers
    {
        public const string CorrelationId = "X-Correlation-Id";
    }

    private static class ContextKeys
    {
        public const string CorrelationId = "CorrelationId";
    }

    private readonly RequestDelegate _next;

    /// <summary>Initializes a new <see cref="CorrelationIdMiddleware"/>.</summary>
    public CorrelationIdMiddleware(RequestDelegate next) => _next = next;

    /// <summary>Extracts or generates a correlation id and enriches the request pipeline with it.</summary>
    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = context.Request.Headers[Headers.CorrelationId].FirstOrDefault()
                            ?? Guid.NewGuid().ToString();

        context.Items[ContextKeys.CorrelationId] = correlationId;
        context.Response.Headers[Headers.CorrelationId] = correlationId;

        await _next(context);
    }
}
