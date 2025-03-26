using Amolenk.Admitto.Application.Common.Abstractions;

namespace Amolenk.Admitto.ApiService.Middleware;

public class UnitOfWorkMiddleware(RequestDelegate next)
{
    public async Task Invoke(HttpContext context, IUnitOfWork unitOfWork)
    {
        await next(context);
        await unitOfWork.SaveChangesAsync();
    }
}