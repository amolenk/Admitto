using Amolenk.Admitto.Application.Common;
using Microsoft.AspNetCore.Diagnostics;

namespace Amolenk.Admitto.ApiService.Middleware;

public class ApplicationRuleExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, 
        CancellationToken cancellationToken)
    {
        if (exception is not ApplicationRuleException applicationRuleException) return false;

        var problemDetails = applicationRuleException.ToProblemDetails(httpContext);

        var result = Results.Problem(problemDetails);
        await result.ExecuteAsync(httpContext);

        return true;
    }
}