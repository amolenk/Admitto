using Amolenk.Admitto.Core.Shared.Application.Auth;
using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Amolenk.Admitto.Core.Shared.Application.Persistence;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.EmailVerification.VerifyOtp.PartnerApi;

public static class VerifyOtpHttpEndpoint
{
    public static RouteGroupBuilder MapVerifyOtp(this RouteGroupBuilder group)
    {
        group.MapPost("/otp/verify", VerifyOtp)
            .WithName(nameof(VerifyOtp))
            .RequireRateLimiting("public-strict");

        return group;
    }

    private static async ValueTask<IResult> VerifyOtp(
        HttpContext httpContext,
        Guid eventId,
        VerifyOtpHttpRequest request,
        ICommandHandler<VerifyOtpCommand, string> handler,
        [FromKeyedServices(RegistrationsModule.Key)]
        IUnitOfWork unitOfWork,
        CancellationToken cancellationToken)
    {
        var teamId = httpContext.User.GetRequiredTeamId();
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
