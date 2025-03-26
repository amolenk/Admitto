using Amolenk.Admitto.Application.UseCases.Auth.SendMagicLink;
using Microsoft.AspNetCore.Mvc;

namespace Amolenk.Admitto.ApiService.Endpoints;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/authorize", SendMagicLink)
            .WithName(nameof(SendMagicLink))
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest);
    }

    private static async Task<IResult> SendMagicLink([FromBody] SendMagicLinkCommand command, 
        [FromServices] SendMagicLinkHandler handler)
    {
        await handler.HandleAsync(command, CancellationToken.None);

        return Results.Ok();
    }
}