using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Amolenk.Admitto.Core.Shared.Application.Persistence;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.EmailVerification.RequestOtp.PublicApi;

public static class RequestOtpHttpEndpoint
{
    public static RouteGroupBuilder MapRequestOtp(this RouteGroupBuilder group)
    {
        group.MapPost("/otp/request", HandleAsync)
            .WithName(nameof(RequestOtpHttpEndpoint));

        return group;
    }

    private static async ValueTask<IResult> HandleAsync(
        Guid teamId,
        Guid eventId,
        RequestOtpHttpRequest request,
        IMediator mediator,
        [FromKeyedServices(RegistrationsModule.Key)]
        IUnitOfWork unitOfWork,
        CancellationToken cancellationToken)
    {
        var command = new RequestOtpCommand(
            TeamId.From(teamId),
            TicketedEventId.From(eventId),
            request.Email);

        await mediator.SendAsync(command, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Results.Accepted();
    }
}
