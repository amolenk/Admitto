using Amolenk.Admitto.Application.Common.Abstractions;
using Amolenk.Admitto.Domain.Exceptions;
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
            return Results.Problem("The item that you tried to create already exists.", 
                statusCode: StatusCodes.Status409Conflict);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Results.Problem("The item you tried to update was changed by another user.",
                statusCode: StatusCodes.Status409Conflict);
        }
    }
}