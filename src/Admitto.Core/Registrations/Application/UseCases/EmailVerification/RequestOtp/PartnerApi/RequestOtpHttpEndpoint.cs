using Amolenk.Admitto.Core.Shared.Application.Auth;
using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Amolenk.Admitto.Core.Shared.Application.Persistence;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.EmailVerification.RequestOtp.PartnerApi;

public static class RequestOtpHttpEndpoint
{
    public static RouteGroupBuilder MapRequestOtp(this RouteGroupBuilder group)
    {
        group.MapPost("/otp/request", RequestOtp)
            .WithName(nameof(RequestOtp))
            .RequireRateLimiting("public-strict");

        return group;
    }

    private static async ValueTask<IResult> RequestOtp(
        HttpContext httpContext,
        Guid eventId,
        RequestOtpHttpRequest request,
        ICommandHandler<RequestOtpCommand> handler,
        [FromKeyedServices(RegistrationsModule.Key)]
        IUnitOfWork unitOfWork,
        CancellationToken cancellationToken)
    {
        var teamId = httpContext.User.GetRequiredTeamId();
        var command = new RequestOtpCommand(
            TeamId.From(teamId),
            TicketedEventId.From(eventId),
            request.Email);

        await handler.HandleAsync(command, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Results.Accepted();
    }
}
