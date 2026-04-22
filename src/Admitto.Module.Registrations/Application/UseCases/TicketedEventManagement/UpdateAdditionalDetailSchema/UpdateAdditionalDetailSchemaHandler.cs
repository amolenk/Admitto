using Amolenk.Admitto.Module.Registrations.Application.Persistence;
using Amolenk.Admitto.Module.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Module.Shared.Application.Messaging;
using Amolenk.Admitto.Module.Shared.Application.Persistence;

namespace Amolenk.Admitto.Module.Registrations.Application.UseCases.TicketedEventManagement.UpdateAdditionalDetailSchema;

internal sealed class UpdateAdditionalDetailSchemaHandler(IRegistrationsWriteStore writeStore)
    : ICommandHandler<UpdateAdditionalDetailSchemaCommand>
{
    public async ValueTask HandleAsync(
        UpdateAdditionalDetailSchemaCommand command,
        CancellationToken cancellationToken)
    {
        var ticketedEvent = await writeStore.TicketedEvents.GetAsync(
            command.EventId,
            command.ExpectedVersion,
            cancellationToken);

        var fields = command.Fields
            .Select(f => AdditionalDetailField.Create(f.Key, f.Name, f.MaxLength))
            .ToArray();

        ticketedEvent.UpdateAdditionalDetailSchema(fields);
    }
}
