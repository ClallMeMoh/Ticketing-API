using System.Text.Json;
using FluentValidation;
using Ticketing.Application.Exceptions;
using Ticketing.Domain.Exceptions;

namespace Ticketing.API.Middleware;

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
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception occurred.");
            await HandleExceptionAsync(context, ex);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var (statusCode, response) = exception switch
        {
            NotFoundException ex => (StatusCodes.Status404NotFound, new ErrorResponse("Not Found", ex.Message)),
            ForbiddenAccessException ex => (StatusCodes.Status403Forbidden, new ErrorResponse("Forbidden", ex.Message)),
            UnauthorizedException ex => (StatusCodes.Status401Unauthorized, new ErrorResponse("Unauthorized", ex.Message)),
            DomainException ex => (StatusCodes.Status400BadRequest, new ErrorResponse("Bad Request", ex.Message)),
            ValidationException ex => (StatusCodes.Status400BadRequest, new ErrorResponse("Validation Failed",
                string.Join("; ", ex.Errors.Select(e => e.ErrorMessage)))),
            UnauthorizedAccessException => (StatusCodes.Status401Unauthorized, new ErrorResponse("Unauthorized",
                "You are not authenticated.")),
            _ => (StatusCodes.Status500InternalServerError, new ErrorResponse("Internal Server Error",
                "An unexpected error occurred."))
        };

        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";

        var json = JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await context.Response.WriteAsync(json);
    }
}

public record ErrorResponse(string Error, string Message);
