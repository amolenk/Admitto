using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Amolenk.Admitto.ApiService.Middleware;

public class GlobalExceptionHandler(IHostEnvironment environment, ILogger<GlobalExceptionHandler> logger)
    : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, 
        CancellationToken cancellationToken)
    {
        logger.LogError(exception, "An unhandled exception occurred.");

        var problemDetails = new ProblemDetails
        {
            Status = StatusCodes.Status500InternalServerError,
            Title = "Internal Server Error",
            Detail = environment.IsDevelopment()
                ? exception.ToString()
                : "An unexpected error occurred. Please try again later.",
            Instance = httpContext.Request.Path
        };

        var result = Results.Problem(problemDetails);
        await result.ExecuteAsync(httpContext);

        return true;
    }
}
