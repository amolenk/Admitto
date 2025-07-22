using Amolenk.Admitto.Domain;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Amolenk.Admitto.ApiService.Middleware;

public class BusinessRuleExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, 
        CancellationToken cancellationToken)
    {
        if (exception is not BusinessRuleException businessRuleException) return false;

        var problemDetails = new ProblemDetails
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "Business Rule Violation",
            Detail = exception.Message,
            Instance = httpContext.Request.Path
        };
        
        problemDetails.Extensions.Add("errorCode", businessRuleException.ErrorCode);

        var result = Results.Problem(problemDetails);
        await result.ExecuteAsync(httpContext);

        return true;
    }
}