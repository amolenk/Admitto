using Amolenk.Admitto.ApiService.Middleware;
using Amolenk.Admitto.Application.UseCases.Email.ConfigureEventEmailTemplate;
using Amolenk.Admitto.Application.UseCases.TicketedEvents.AddTicketType;
using Amolenk.Admitto.Application.UseCases.TicketedEvents.CreateTicketedEvent;
using Amolenk.Admitto.Application.UseCases.TicketedEvents.GetTicketedEvent;
using Amolenk.Admitto.Application.UseCases.TicketedEvents.GetTicketedEvents;
using Amolenk.Admitto.Application.UseCases.TicketedEvents.GetTicketTypes;

namespace Amolenk.Admitto.ApiService.Endpoints;

public static class TicketedEventEndpoints
{
    public static void MapTicketedEventEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/teams/{teamSlug}/events")
            .WithTags("Events")
            .AddEndpointFilter<ValidationFilter>()
            .AddEndpointFilter<UnitOfWorkFilter>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status500InternalServerError)
            .RequireAuthorization();

        group
            .MapAddTicketType()
            .MapCreateTicketedEvent()
            .MapGetTicketedEvent()
            .MapGetTicketedEvents()
            .MapGetTicketTypes();
    }
}