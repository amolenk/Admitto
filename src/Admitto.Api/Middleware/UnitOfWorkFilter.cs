using Amolenk.Admitto.Application.Common.Abstractions;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Amolenk.Admitto.ApiService.Middleware;

public class UnitOfWorkFilter(ILogger<UnitOfWorkFilter> logger) : IEndpointFilter
{
    private const string PostgresUniqueViolation = "23505";
    
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var unitOfWork = context.HttpContext.RequestServices.GetRequiredService<IUnitOfWork>();
        
        try
        {
            var result = await next(context);
            await unitOfWork.SaveChangesAsync();
            return result;
        }
        catch (DbUpdateException e) when (e.InnerException is PostgresException { SqlState: PostgresUniqueViolation })
        {
            return Results.Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Conflict",
                detail: "The item that you tried to create already exists.", 
                instance: context.HttpContext.Request.Path);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Results.Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Conflict",
                detail: "The item you tried to update was changed by another user.",
                instance: context.HttpContext.Request.Path);
        }
    }
}