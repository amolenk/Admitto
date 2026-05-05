using Amolenk.Admitto.Module.Email.Domain.ValueObjects;
using Amolenk.Admitto.Module.Shared.Application.Auth;
using Amolenk.Admitto.Module.Shared.Application.Http;
using Amolenk.Admitto.Module.Shared.Application.Messaging;
using Amolenk.Admitto.Module.Shared.Kernel.ErrorHandling;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Module.Email.Application.UseCases.EmailTemplates.TestSendEmailTemplate.AdminApi;

public static class TestSendEmailTemplateHttpEndpoint
{
    private static readonly HashSet<string> KnownTypes =
    [
        EmailTemplateType.Ticket,
        EmailTemplateType.Reconfirm,
        EmailTemplateType.Cancellation,
        EmailTemplateType.VisaLetterDenied,
        EmailTemplateType.OtpCode,
    ];

    private static readonly Error UnknownTemplateType = new(
        "email_template.unknown_type",
        "The specified template type is not supported.");

    public static RouteGroupBuilder MapTestSendEmailTemplate(
        this RouteGroupBuilder group,
        bool isEventScoped)
    {
        var endpointName = isEventScoped
            ? "TestSendEventEmailTemplate"
            : "TestSendTeamEmailTemplate";

        group
            .MapPost("/test-send", async (
                string teamSlug,
                string? eventSlug,
                string type,
                IOrganizationScopeResolver scopeResolver,
                TestSendEmailTemplateHttpRequest request,
                IMediator mediator,
                CancellationToken ct) =>
            {
                if (!KnownTypes.Contains(type))
                    throw new BusinessRuleViolationException(UnknownTemplateType);

                var orgScope = await scopeResolver.ResolveAsync(teamSlug, eventSlug, ct);

                var command = isEventScoped
                    ? request.ToCommand(
                        TeamId.From(orgScope.TeamId),
                        TicketedEventId.From(orgScope.EventId!.Value),
                        type)
                    : request.ToCommand(
                        TeamId.From(orgScope.TeamId),
                        null,
                        type);

                await mediator.SendAsync(command, ct);
                return TypedResults.Ok();
            })
            .WithName(endpointName)
            .RequireAuthorization(policy => policy.RequireTeamMembership(TeamMembershipRole.Organizer));

        return group;
    }
}
