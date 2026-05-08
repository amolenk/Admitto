using Amolenk.Admitto.Module.Shared.Application.Messaging;
using Amolenk.Admitto.Module.Shared.Application.Persistence;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Module.Registrations.Application.UseCases.EmailVerification.VerifyOtp.PublicApi;

public static class VerifyOtpHttpEndpoint
{
    public static RouteGroupBuilder MapVerifyOtp(this RouteGroupBuilder group)
    {
        group.MapPost("/otp/verify", HandleAsync)
            .WithName(nameof(VerifyOtpHttpEndpoint));

        return group;
    }

    private static async ValueTask<IResult> HandleAsync(
        Guid teamId,
        Guid eventId,
        VerifyOtpHttpRequest request,
        IMediator mediator,
        [FromKeyedServices(RegistrationsModule.Key)]
        IUnitOfWork unitOfWork,
        CancellationToken cancellationToken)
    {
        var command = new VerifyOtpCommand(
            TeamId.From(teamId),
            TicketedEventId.From(eventId),
            request.Email,
            request.Code);

        var token = await mediator.SendReceiveAsync<VerifyOtpCommand, string>(command, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Results.Ok(new { token });
    }
}
