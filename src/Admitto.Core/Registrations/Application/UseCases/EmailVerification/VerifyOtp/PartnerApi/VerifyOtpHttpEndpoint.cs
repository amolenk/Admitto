using Amolenk.Admitto.Core.Registrations.Application.UseCases.TicketedEvents.ResolvePartnerTicketedEvent.PartnerApi;
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
        string eventSlug,
        VerifyOtpHttpRequest request,
        PartnerTicketedEventResolver eventResolver,
        ICommandHandler<VerifyOtpCommand, string> handler,
        [FromKeyedServices(RegistrationsModule.Key)]
        IUnitOfWork unitOfWork,
        CancellationToken cancellationToken)
    {
        var teamId = httpContext.User.GetRequiredTeamId();
        var eventId = await eventResolver.ResolveAsync(TeamId.From(teamId), eventSlug, cancellationToken);
        var command = new VerifyOtpCommand(
            TeamId.From(teamId),
            eventId,
            request.Email,
            request.Code);

        var token = await handler.HandleAsync(command, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Results.Ok(new { token });
    }
}
