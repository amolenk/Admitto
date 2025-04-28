using System.Text.Json;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Amolenk.Admitto.ApiService.Middleware;

public class DbExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        ProblemDetails? problemDetails = null;

        switch (exception)
        {
            case DbUpdateConcurrencyException:
                problemDetails = new ProblemDetails
                {
                    Status = StatusCodes.Status409Conflict,
                    Title = "Concurrency conflict.",
                    Detail = "A concurrency conflict occurred while saving changes.",
                    Instance = httpContext.Request.Path
                };
                httpContext.Response.StatusCode = StatusCodes.Status409Conflict;
                break;
        
            case DbUpdateException dbUpdateException:
                problemDetails = new ProblemDetails
                {
                    Status = StatusCodes.Status400BadRequest,
                    Title = "Database update error.",
                    Detail = dbUpdateException.InnerException?.Message ?? dbUpdateException.Message,
                    Instance = httpContext.Request.Path
                };
                httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
                break;


        }

        if (problemDetails is null) return false;
        
        httpContext.Response.ContentType = "application/json";
        
        await httpContext.Response.WriteAsync(JsonSerializer.Serialize(problemDetails, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        }), cancellationToken);

        return true;
    }
}