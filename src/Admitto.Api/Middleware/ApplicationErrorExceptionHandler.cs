using Amolenk.Admitto.Shared.Application.Http;
using Amolenk.Admitto.Shared.Kernel.ErrorHandling;
using Microsoft.AspNetCore.Diagnostics;

namespace Amolenk.Admitto.ApiService.Middleware;

public class ApplicationErrorExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, 
        CancellationToken cancellationToken)
    {
        if (exception is not BusinessRuleViolationException applicationErrorException) return false;

        var result = applicationErrorException.Error.ToProblemHttpResult();

        await result.ExecuteAsync(httpContext);

        return true;
    }
}