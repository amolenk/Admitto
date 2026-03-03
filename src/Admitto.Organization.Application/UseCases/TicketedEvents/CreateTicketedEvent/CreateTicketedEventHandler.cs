using Amolenk.Admitto.Organization.Application.Persistence;
using Amolenk.Admitto.Organization.Domain.Entities;
using Amolenk.Admitto.Organization.Domain.ValueObjects;
using Amolenk.Admitto.Shared.Application.Messaging;
using Amolenk.Admitto.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Organization.Application.UseCases.TicketedEvents.CreateTicketedEvent;

internal sealed class CreateTicketedEventHandler(IOrganizationWriteStore writeStore)
    : ICommandHandler<CreateTicketedEventCommand>
{
    public ValueTask HandleAsync(CreateTicketedEventCommand command, CancellationToken cancellationToken)
    {
        var ticketedEvent = TicketedEvent.Create(
            TeamId.From(command.TeamId),
            Slug.From(command.Slug),
            DisplayName.From(command.Name),
            AbsoluteUrl.From(command.WebsiteUrl),
            AbsoluteUrl.From(command.BaseUrl),
            new TimeWindow(command.StartsAt, command.EndsAt));

        writeStore.TicketedEvents.Add(ticketedEvent);

        return ValueTask.CompletedTask;
    }
}
