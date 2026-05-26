using Amolenk.Admitto.Core.Registrations.Application.Persistence;
using Amolenk.Admitto.Core.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Amolenk.Admitto.Core.Shared.Application.Persistence;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.TicketTypeManagement.UpdateTicketType;

internal sealed class UpdateTicketTypeHandler(IRegistrationsWriteStore writeStore)
    : ICommandHandler<UpdateTicketTypeCommand>
{
    public async ValueTask HandleAsync(
        UpdateTicketTypeCommand command,
        CancellationToken cancellationToken)
    {
        TicketedEventId eventId = TicketedEventId.From(command.EventId);
        TicketTypeId ticketTypeId = TicketTypeId.From(command.TicketTypeId);
        TicketTypeName? name = command.Name is not null ? TicketTypeName.From(command.Name) : null;

        var catalog = await writeStore.TicketCatalogs.GetAsync(
             eventId,
             cancellationToken);

        catalog.UpdateTicketType(ticketTypeId, name, command.MaxCapacity, command.SelfServiceEnabled,
            command.WaitlistEnabled, command.ClaimWindowHours, command.MaxReconfirmAttempts, command.UpdateMaxReconfirmAttempts);
    }
}

