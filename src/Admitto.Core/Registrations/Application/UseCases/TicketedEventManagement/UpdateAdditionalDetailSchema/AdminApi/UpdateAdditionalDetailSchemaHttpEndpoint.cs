using Amolenk.Admitto.Core.Shared.Application.Auth;
using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Amolenk.Admitto.Core.Shared.Application.Persistence;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.TicketedEventManagement.UpdateAdditionalDetailSchema.AdminApi;

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
        Guid teamId,
        Guid eventId,
        UpdateAdditionalDetailSchemaHttpRequest request,
        IMediator mediator,
        [FromKeyedServices(RegistrationsModule.Key)]
        IUnitOfWork unitOfWork,
        CancellationToken cancellationToken)
    {
        var fields = (request.Fields ?? [])
            .Select(f => new UpdateAdditionalDetailSchemaCommand.FieldInput(f.Key, f.Name, f.MaxLength))
            .ToArray();

        var command = new UpdateAdditionalDetailSchemaCommand(
            eventId,
            request.ExpectedVersion,
            fields);

        await mediator.SendAsync(command, cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return TypedResults.NoContent();
    }
}
