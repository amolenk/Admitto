// using Amolenk.Admitto.Core.Organization.Domain.DomainEvents;
// using Amolenk.Admitto.Core.Shared.Application.Messaging;
//
// namespace Amolenk.Admitto.Core.Organization.Application.Persistence.ReadModel.TicketedEventMembers;
//
// public class TicketedEventMembersProjection(IOrganizationWriteStore writeStore, IOrganizationReadStore readStore) :
//     IDomainEventHandler<TicketedEventCreationRequestedDomainEvent>,
//     IDomainEventHandler<TeamMembershipAssignedDomainEvent>,
//     IDomainEventHandler<TeamMembershipChangedDomainEvent>,
//     IDomainEventHandler<TeamMembershipRemovedDomainEvent>
// {
//     // public ValueTask HandleAsync(UserCreatedDomainEvent domainEvent, CancellationToken cancellationToken)
//     // {
//     //     domainEvent.
//     //
//     //     throw new NotImplementedException();
//     //
//     //
//     //     var record = new TeamMemberView
//     //     {
//     //         UserId = domainEvent.Member.Id,
//     //         TeamId = domainEvent.TeamId,
//     //         Role = domainEvent.Member.Role,
//     //         AssignedAt = domainEvent.OccurredOn
//     //     };
//     //
//     //     context.TeamMemberView.Add(record);
//     //
//     //     return ValueTask.CompletedTask;
//     // }
//
//
//     // Team ID     | Event ID
//     // ------------------------------
//     // Azure Fest  | Azure Fest 2025
//     // Azure Fest  | Azure Fest 2026
//
//
//     // Team ID     | User ID | Role
//     // ---------------------------------
//     // Azure Fest  | Sander  | Organizer
//     // Azure Fest  | Patrick | Crew
//
//     // JOIN ON TeamId
//
//
//     public ValueTask HandleAsync(
//         TicketedEventCreationRequestedDomainEvent domainEvent,
//         CancellationToken cancellationToken)
//     {
//         domainEvent.
//     }
//
//     public async ValueTask HandleAsync(TeamMembershipAssignedDomainEvent domainEvent, CancellationToken cancellationToken)
//     {
//         var eventRecords = await readStore.TicketedEventMembersView
//             .Where(v => v.TeamId == domainEvent.TeamId.Value)
//             .ToListAsync(cancellationToken);
//
//         foreach (var record in eventRecords)
//         {
//             record.
//         }
//
//         throw new NotImplementedException();
//     }
//
//     public ValueTask HandleAsync(TeamMembershipChangedDomainEvent domainEvent, CancellationToken cancellationToken)
//     {
//         throw new NotImplementedException();
//     }
//
//     public ValueTask HandleAsync(TeamMembershipRemovedDomainEvent domainEvent, CancellationToken cancellationToken)
//     {
//         throw new NotImplementedException();
//     }
// }
