using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using TemplateMs.Application.Exceptions;
using TemplateMs.Domain.Exceptions;

namespace TemplateMs.Api.Filters;

/// <summary>
/// ASP.NET Core <see cref="IExceptionHandler"/> that maps known exception types to RFC 7807
/// <see cref="ProblemDetails"/> responses. Registered via <c>AddExceptionHandler</c> in DI.
/// </summary>
public sealed class GlobalExceptionFilter : IExceptionHandler
{
    private static class Titles
    {
        public const string Validation = "Validation Failure";
        public const string Domain = "Business Rule Violation";
        public const string NotFound = "Resource Not Found";
        public const string Internal = "Internal Server Error";
    }

    private readonly ILogger<GlobalExceptionFilter> _logger;

    /// <summary>Initializes a new <see cref="GlobalExceptionFilter"/>.</summary>
    public GlobalExceptionFilter(ILogger<GlobalExceptionFilter> logger) => _logger = logger;

    /// <inheritdoc />
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var correlationId = httpContext.Items["CorrelationId"]?.ToString() ?? string.Empty;

        _logger.LogError(exception, "Unhandled exception. CorrelationId: {CorrelationId}", correlationId);

        var (status, title, extensions) = exception switch
        {
            ValidationException ve => (StatusCodes.Status400BadRequest, Titles.Validation,
                (object)new { errors = ve.Errors, correlationId }),
            NotFoundException => (StatusCodes.Status404NotFound, Titles.NotFound,
                (object)new { correlationId }),
            DomainException => (StatusCodes.Status422UnprocessableEntity, Titles.Domain,
                (object)new { correlationId }),
            _ => (StatusCodes.Status500InternalServerError, Titles.Internal,
                (object)new { correlationId })
        };

        httpContext.Response.StatusCode = status;

        var problem = new ProblemDetails
        {
            Status = status,
            Title = title,
            Detail = exception.Message,
            Extensions = { ["correlationId"] = correlationId }
        };

        if (exception is ValidationException validationEx)
        {
            problem.Extensions["errors"] = validationEx.Errors;
        }

        await httpContext.Response.WriteAsJsonAsync(problem, cancellationToken);
        return true;
    }
}
