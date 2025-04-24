using System.Text.Json;
using Amolenk.Admitto.Application.Common.Abstractions;
using Microsoft.AspNetCore.Mvc;

namespace Amolenk.Admitto.ApiService.Middleware;

public class UnitOfWorkMiddleware(RequestDelegate next)
{
    public async Task Invoke(HttpContext context, IUnitOfWork unitOfWork, ILogger<UnitOfWorkMiddleware> logger)
    {
        await next(context);

        try
        {
            await unitOfWork.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while saving changes to the database.");
            
            // TODO Create a helper for ProblemDetails?
            var problemDetails = new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "An error occurred while saving changes.",
                Detail = ex.Message
            };

            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            context.Response.ContentType = "application/json";

            var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
            await context.Response.WriteAsync(JsonSerializer.Serialize(problemDetails, options));
        }    
    }
}