using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Http;

namespace RLApp.Adapters.Http.Middleware;

public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;

    public GlobalExceptionMiddleware(RequestDelegate next)
    {
        _next = next;
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

    private static Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";

        var response = new 
        {
            Title = "An error occurred while processing your request.",
            Status = (int)HttpStatusCode.InternalServerError,
            Detail = exception.Message
        };

        // If DomainException or ApplicationException exists, map appropriately (e.g. 400 Bad Request, 404 Not Found)
        if (exception.GetType().Name.Contains("DomainException", StringComparison.OrdinalIgnoreCase) ||
            exception.GetType().Name.Contains("ApplicationException", StringComparison.OrdinalIgnoreCase))
        {
            response = new 
            {
                Title = "Domain validation failed.",
                Status = (int)HttpStatusCode.BadRequest,
                Detail = exception.Message
            };
            context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
        }
        else
        {
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
        }

        return context.Response.WriteAsync(JsonSerializer.Serialize(response));
    }
}
