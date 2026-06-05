using Amolenk.Admitto.Core.Registrations.Application.Persistence;
using Amolenk.Admitto.Core.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Amolenk.Admitto.Core.Shared.Application.Persistence;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.TicketedEvents.UpdateAdditionalDetailSchema;

internal sealed class UpdateAdditionalDetailSchemaHandler(IRegistrationsWriteStore writeStore)
    : ICommandHandler<UpdateAdditionalDetailSchemaCommand>
{
    public async ValueTask HandleAsync(
        UpdateAdditionalDetailSchemaCommand command,
        CancellationToken cancellationToken)
    {
        TicketedEventId eventId = TicketedEventId.From(command.EventId);
        TeamId teamId = TeamId.From(command.TeamId);

        var ticketedEvent = await writeStore.TicketedEvents.GetAsync(
            e => e.Id == eventId && e.TeamId == teamId,
            command.ExpectedVersion,
            cancellationToken);

        var fields = command.Fields
            .Select(f => AdditionalDetailField.Create(f.Key, f.Name, f.MaxLength))
            .ToArray();

        ticketedEvent.UpdateAdditionalDetailSchema(fields);
    }
}
