using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Amolenk.Admitto.Core.Shared.Application.Persistence;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.EmailVerification.VerifyOtp.PublicApi;

public static class VerifyOtpHttpEndpoint
{
    public static RouteGroupBuilder MapVerifyOtp(this RouteGroupBuilder group)
    {
        group.MapPost("/otp/verify", VerifyOtp)
            .WithName(nameof(VerifyOtp));

        return group;
    }

    private static async ValueTask<IResult> VerifyOtp(
        Guid teamId,
        Guid eventId,
        VerifyOtpHttpRequest request,
        ICommandHandler<VerifyOtpCommand, string> handler,
        [FromKeyedServices(RegistrationsModule.Key)]
        IUnitOfWork unitOfWork,
        CancellationToken cancellationToken)
    {
        var command = new VerifyOtpCommand(
            TeamId.From(teamId),
            TicketedEventId.From(eventId),
            request.Email,
            request.Code);

        var token = await handler.HandleAsync(command, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Results.Ok(new { token });
    }
}
