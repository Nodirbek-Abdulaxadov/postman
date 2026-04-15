using System.Net;
using System.Text.Json;
using PostalDeliverySystem.Application.Common.Exceptions;

namespace PostalDeliverySystem.Api.Middleware;

public sealed class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
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
            await HandleExceptionAsync(context, exception);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var (statusCode, message) = exception switch
        {
            ApplicationValidationException validationException
                => (HttpStatusCode.BadRequest, validationException.Message),
            ApplicationUnauthorizedException unauthorizedException
                => (HttpStatusCode.Unauthorized, unauthorizedException.Message),
            ApplicationForbiddenException forbiddenException
                => (HttpStatusCode.Forbidden, forbiddenException.Message),
            ApplicationNotFoundException notFoundException
                => (HttpStatusCode.NotFound, notFoundException.Message),
            ApplicationConflictException conflictException
                => (HttpStatusCode.Conflict, conflictException.Message),
            _ => (HttpStatusCode.InternalServerError, "An unexpected error occurred.")
        };

        if ((int)statusCode >= 500)
        {
            _logger.LogError(exception, "Unhandled exception while processing request {Path}", context.Request.Path);
        }

        context.Response.StatusCode = (int)statusCode;
        context.Response.ContentType = "application/json";

        var payload = JsonSerializer.Serialize(new
        {
            error = message
        });

        await context.Response.WriteAsync(payload);
    }
}