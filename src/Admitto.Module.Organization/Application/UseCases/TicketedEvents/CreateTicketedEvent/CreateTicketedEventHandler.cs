using Amolenk.Admitto.Module.Organization.Application.Persistence;
using Amolenk.Admitto.Module.Organization.Domain.Entities;
using Amolenk.Admitto.Module.Organization.Domain.ValueObjects;
using Amolenk.Admitto.Module.Shared.Application.Messaging;
using Amolenk.Admitto.Module.Shared.Application.Persistence;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Module.Organization.Application.UseCases.TicketedEvents.CreateTicketedEvent;

/// <summary>
/// Creates a new <see cref="TicketedEvent"/> under the specified team.
/// </summary>
/// <remarks>
/// <para>
/// The handler first loads the team to perform a fast-fail archived check. This path also
/// loads the entity into the EF change tracker, so the domain event handler that runs during
/// <c>SaveChanges</c> gets the same tracked instance at zero extra database cost.
/// </para>
/// <para>
/// <see cref="TicketedEvent.Create"/> raises a <c>TicketedEventCreatedDomainEvent</c>.
/// <c>DomainEventsInterceptor</c> dispatches it synchronously inside <c>SavingChangesAsync</c>,
/// where <c>TicketedEventCreatedDomainEventHandler</c> calls
/// <c>team.RegisterTicketedEventCreation()</c> to increment <c>TicketedEventScopeVersion</c>.
/// This forces a write to the team row, advancing its EF row-version concurrency token so
/// that any racing <c>ArchiveTeam</c> operation detects the conflict.
/// </para>
/// </remarks>
internal sealed class CreateTicketedEventHandler(IOrganizationWriteStore writeStore)
    : ICommandHandler<CreateTicketedEventCommand>
{
    public async ValueTask HandleAsync(CreateTicketedEventCommand command, CancellationToken cancellationToken)
    {
        // Load the team to fast-fail if it is archived. The tracked entity is reused by
        // TicketedEventCreatedDomainEventHandler during SaveChanges at no extra DB cost.
        var team = await writeStore.Teams.GetAsync(TeamId.From(command.TeamId), cancellationToken);
        team.EnsureNotArchived();

        var ticketedEvent = TicketedEvent.Create(
            TeamId.From(command.TeamId),
            Slug.From(command.Slug),
            DisplayName.From(command.Name),
            AbsoluteUrl.From(command.WebsiteUrl),
            AbsoluteUrl.From(command.BaseUrl),
            new TimeWindow(command.StartsAt, command.EndsAt));

        writeStore.TicketedEvents.Add(ticketedEvent);
    }
}
