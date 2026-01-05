using Amolenk.Admitto.Application.Common.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Amolenk.Admitto.ApiService.Middleware;

public class UnitOfWorkFilter : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var unitOfWork = context.HttpContext.RequestServices.GetRequiredService<IUnitOfWork>();
        
        try
        {
            var result = await next(context);
            await unitOfWork.SaveChangesAsync();
            
            return result;
        }
        catch (DbUpdateConcurrencyException)
        {
            // TODO Move to UnitOfWork 
            // TODO Use BusinessRuleError to get code and message instead of hardcoding them
            return Results.Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Conflict occured",
                detail: "The item you tried to update was changed by another user.",
                instance: context.HttpContext.Request.Path);
        }
    }
}