using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RLApp.Domain.Common;

namespace RLApp.Adapters.Http.Middleware;

public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;
    private readonly IHostEnvironment _environment;

    public GlobalExceptionMiddleware(
        RequestDelegate next,
        ILogger<GlobalExceptionMiddleware> logger,
        IHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        _logger.LogError(exception, "Unhandled exception while processing request {Path}", context.Request.Path);

        var (statusCode, title, detail) = exception switch
        {
            DomainException => ((int)HttpStatusCode.BadRequest, "Domain validation failed.", exception.Message),
            KeyNotFoundException => ((int)HttpStatusCode.NotFound, "Resource not found.", exception.Message),
            UnauthorizedAccessException => ((int)HttpStatusCode.Forbidden, "Access denied.", "You are not allowed to perform this operation."),
            _ => ((int)HttpStatusCode.InternalServerError, "An unexpected error occurred.", _environment.IsDevelopment() ? exception.Message : "The server could not process the request.")
        };

        context.Response.ContentType = "application/problem+json";
        context.Response.StatusCode = statusCode;

        var problem = new ProblemDetails
        {
            Title = title,
            Status = statusCode,
            Detail = detail,
            Instance = context.Request.Path
        };

        problem.Extensions["traceId"] = context.TraceIdentifier;
        if (context.Request.Headers.TryGetValue("X-Correlation-Id", out var correlationId))
        {
            problem.Extensions["correlationId"] = correlationId.ToString();
        }

        return context.Response.WriteAsync(JsonSerializer.Serialize(problem));
    }
}
