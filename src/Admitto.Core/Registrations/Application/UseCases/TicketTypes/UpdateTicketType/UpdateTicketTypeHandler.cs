using Amolenk.Admitto.Core.Registrations.Application.Persistence;
using Amolenk.Admitto.Core.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Amolenk.Admitto.Core.Shared.Application.Persistence;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.TicketTypes.UpdateTicketType;

internal sealed class UpdateTicketTypeHandler(IRegistrationsWriteStore writeStore)
    : ICommandHandler<UpdateTicketTypeCommand>
{
    public async ValueTask HandleAsync(
        UpdateTicketTypeCommand command,
        CancellationToken cancellationToken)
    {
        TicketedEventId eventId = TicketedEventId.From(command.EventId);
        TeamId teamId = TeamId.From(command.TeamId);
        TicketTypeId ticketTypeId = TicketTypeId.From(command.TicketTypeId);
        TicketTypeName? name = command.Name is not null ? TicketTypeName.From(command.Name) : null;
        ReconfirmationEmailLimit? reconfirmationEmailLimit = command.UpdateMaxReconfirmationEmails
            ? command.MaxReconfirmationEmails is null
                ? null
                : ReconfirmationEmailLimit.From(command.MaxReconfirmationEmails.Value)
            : null;

        var catalog = await writeStore.TicketCatalogs.GetAsync(
             tc => tc.Id == eventId && tc.TeamId == teamId,
             cancellationToken);

        catalog.UpdateTicketType(ticketTypeId, name, command.MaxCapacity, command.SelfServiceEnabled,
            command.WaitlistEnabled, command.ClaimWindowHours, reconfirmationEmailLimit,
            command.UpdateMaxReconfirmationEmails);
    }
}
