using Amolenk.Admitto.Application.UseCases.TicketedEvents.CreateTicketedEvent;
using Amolenk.Admitto.Application.UseCases.TicketedEvents.GetActiveTicketedEvents;
using Amolenk.Admitto.Application.UseCases.TicketedEvents.GetTicketedEvent;

namespace Amolenk.Admitto.ApiService.Endpoints;

public static class TicketedEventEndpoints
{
    public static void MapTicketedEventEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/events/v1").WithTags("Events");

        group
            .MapCreateTicketedEvent()
            .MapGetActiveTicketedEvents()
            .MapGetTicketedEvent();
    }
}