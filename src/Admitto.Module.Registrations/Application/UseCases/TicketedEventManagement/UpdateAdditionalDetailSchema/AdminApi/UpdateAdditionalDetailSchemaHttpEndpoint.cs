using Amolenk.Admitto.Module.Shared.Application.Auth;
using Amolenk.Admitto.Module.Shared.Application.Http;
using Amolenk.Admitto.Module.Shared.Application.Messaging;
using Amolenk.Admitto.Module.Shared.Application.Persistence;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Amolenk.Admitto.Module.Registrations.Application.UseCases.TicketedEventManagement.UpdateAdditionalDetailSchema.AdminApi;

public static class UpdateAdditionalDetailSchemaHttpEndpoint
{
    public static RouteGroupBuilder MapUpdateAdditionalDetailSchema(this RouteGroupBuilder group)
    {
        group
            .MapPut("/additional-detail-schema", UpdateAdditionalDetailSchema)
            .WithName(nameof(UpdateAdditionalDetailSchema))
            .RequireAuthorization(policy => policy.RequireTeamMembership(TeamMembershipRole.Organizer));

        return group;
    }

    private static async ValueTask<NoContent> UpdateAdditionalDetailSchema(
        string teamSlug,
        string eventSlug,
        IOrganizationScopeResolver scopeResolver,
        UpdateAdditionalDetailSchemaHttpRequest request,
        IMediator mediator,
        [FromKeyedServices(RegistrationsModule.Key)]
        IUnitOfWork unitOfWork,
        CancellationToken cancellationToken)
    {
        var scope = await scopeResolver.ResolveAsync(teamSlug, eventSlug, cancellationToken);

        var fields = (request.Fields ?? [])
            .Select(f => new UpdateAdditionalDetailSchemaCommand.FieldInput(f.Key, f.Name, f.MaxLength))
            .ToArray();

        var command = new UpdateAdditionalDetailSchemaCommand(
            TicketedEventId.From(scope.EventId!.Value),
            request.ExpectedVersion,
            fields);

        await mediator.SendAsync(command, cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return TypedResults.NoContent();
    }
}
