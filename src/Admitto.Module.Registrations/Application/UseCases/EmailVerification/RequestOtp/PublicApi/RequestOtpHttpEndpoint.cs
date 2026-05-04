using Amolenk.Admitto.Module.Organization.Contracts;
using Amolenk.Admitto.Module.Shared.Application.Http;
using Amolenk.Admitto.Module.Shared.Application.Messaging;
using Amolenk.Admitto.Module.Shared.Application.Persistence;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Module.Registrations.Application.UseCases.EmailVerification.RequestOtp.PublicApi;

public static class RequestOtpHttpEndpoint
{
    public static RouteGroupBuilder MapRequestOtp(this RouteGroupBuilder group)
    {
        group.MapPost("/otp/request", HandleAsync)
            .WithName(nameof(RequestOtpHttpEndpoint));

        return group;
    }

    private static async ValueTask<IResult> HandleAsync(
        string teamSlug,
        string eventSlug,
        RequestOtpHttpRequest request,
        IOrganizationFacade facade,
        ITicketedEventIdLookup ticketedEventIdLookup,
        IMediator mediator,
        [FromKeyedServices(RegistrationsModule.Key)]
        IUnitOfWork unitOfWork,
        CancellationToken cancellationToken)
    {
        var teamId = await facade.GetTeamIdAsync(teamSlug, cancellationToken);
        var eventId = await ticketedEventIdLookup.GetTicketedEventIdAsync(teamId, eventSlug, cancellationToken);

        var command = new RequestOtpCommand(
            TeamId.From(teamId),
            TicketedEventId.From(eventId),
            request.Email);

        await mediator.SendAsync(command, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Results.Accepted();
    }
}
