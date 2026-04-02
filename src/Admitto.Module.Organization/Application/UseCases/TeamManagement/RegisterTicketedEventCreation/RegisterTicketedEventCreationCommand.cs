using Amolenk.Admitto.Module.Shared.Application.Messaging;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Module.Organization.Application.UseCases.TeamManagement.RegisterTicketedEventCreation;

/// <summary>
/// Internal command dispatched by <c>TicketedEventCreatedDomainEventHandler</c> when a new
/// ticketed event is created under a team. Increments
/// <see cref="Domain.Entities.Team.TicketedEventScopeVersion"/> to advance the team's EF
/// concurrency token and close the TOCTOU window with concurrent archive operations.
/// </summary>
internal sealed record RegisterTicketedEventCreationCommand(TeamId TeamId) : Command;
