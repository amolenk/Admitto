using Amolenk.Admitto.Core.Registrations.Application.Persistence;
using Amolenk.Admitto.Core.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Amolenk.Admitto.Core.Shared.Application.Persistence;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.TicketedEventManagement.UpdateAdditionalDetailSchema;

internal sealed class UpdateAdditionalDetailSchemaHandler(IRegistrationsWriteStore writeStore)
    : ICommandHandler<UpdateAdditionalDetailSchemaCommand>
{
    public async ValueTask HandleAsync(
        UpdateAdditionalDetailSchemaCommand command,
        CancellationToken cancellationToken)
    {
        TicketedEventId eventId = TicketedEventId.From(command.EventId);

        var ticketedEvent = await writeStore.TicketedEvents.GetAsync(
            eventId,
            command.ExpectedVersion,
            cancellationToken);

        var fields = command.Fields
            .Select(f => AdditionalDetailField.Create(f.Key, f.Name, f.MaxLength))
            .ToArray();

        ticketedEvent.UpdateAdditionalDetailSchema(fields);
    }
}
