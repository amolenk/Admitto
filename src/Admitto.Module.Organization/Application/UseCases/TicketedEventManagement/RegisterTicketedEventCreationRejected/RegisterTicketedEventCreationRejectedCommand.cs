using Amolenk.Admitto.Module.Shared.Application.Messaging;

namespace Amolenk.Admitto.Module.Organization.Application.UseCases.TicketedEventManagement.RegisterTicketedEventCreationRejected;

/// <summary>
/// Marks the corresponding creation request as <c>Rejected</c> and decrements
/// <c>PendingEventCount</c>. Idempotent via <c>Team.RegisterEventCreationRejected</c>.
/// </summary>
internal sealed record RegisterTicketedEventCreationRejectedCommand(
    Guid TeamId,
    Guid CreationRequestId,
    string Reason) : Command;
