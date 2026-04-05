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

        var (statusCode, title, detail, code) = exception switch
        {
            DomainException domainException when domainException.IsConflict
                => ((int)HttpStatusCode.Conflict, "Optimistic concurrency conflict.", domainException.Message, domainException.Code),
            DomainException domainException
                => ((int)HttpStatusCode.BadRequest, "Domain validation failed.", domainException.Message, domainException.Code),
            KeyNotFoundException => ((int)HttpStatusCode.NotFound, "Resource not found.", exception.Message, null),
            UnauthorizedAccessException => ((int)HttpStatusCode.Forbidden, "Access denied.", "You are not allowed to perform this operation.", null),
            _ => ((int)HttpStatusCode.InternalServerError, "An unexpected error occurred.", _environment.IsDevelopment() ? exception.Message : "The server could not process the request.", null)
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
        if (!string.IsNullOrWhiteSpace(code))
        {
            problem.Extensions["code"] = code;
        }

        if (context.Request.Headers.TryGetValue("X-Correlation-Id", out var correlationId))
        {
            problem.Extensions["correlationId"] = correlationId.ToString();
        }

        return context.Response.WriteAsync(JsonSerializer.Serialize(problem));
    }
}
