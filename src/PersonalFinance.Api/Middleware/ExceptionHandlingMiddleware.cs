using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using PersonalFinance.Application.Common.Exceptions;
using ValidationException = PersonalFinance.Application.Common.Exceptions.ValidationException;

namespace PersonalFinance.Api.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception exception)
        {
            await HandleExceptionAsync(context, exception, _logger);
        }
    }

    private static Task HandleExceptionAsync(HttpContext context, Exception exception, ILogger logger)
    {
        var (statusCode, title, errors) = exception switch
        {
            ValidationException validationException => (
                HttpStatusCode.BadRequest,
                "Validation failed",
                (object?)validationException.Errors),
            NotFoundException => (HttpStatusCode.NotFound, exception.Message, null),
            UnauthorizedException => (HttpStatusCode.Unauthorized, exception.Message, null),
            ConflictException => (HttpStatusCode.Conflict, exception.Message, null),
            // e.g. a query/route parameter that fails to bind (malformed Guid, DateTime, ...) —
            // the framework already knows the right status code, don't downgrade it to a 500.
            BadHttpRequestException badRequest => ((HttpStatusCode)badRequest.StatusCode, badRequest.Message, null),
            _ => (HttpStatusCode.InternalServerError, "An unexpected error occurred.", null)
        };

        if (statusCode == HttpStatusCode.InternalServerError)
        {
            logger.LogError(exception, "Unhandled exception while processing {Method} {Path}", context.Request.Method, context.Request.Path);
        }

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)statusCode;

        var payload = JsonSerializer.Serialize(new { title, errors });

        return context.Response.WriteAsync(payload);
    }
}
