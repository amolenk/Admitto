using Amolenk.Admitto.Core.Registrations.Contracts;
using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.Registrations.GetRegistrations;

/// <summary>
/// Returns registrations for a ticketed event with optional filtering.
/// When <paramref name="Filter"/> is provided only registrations matching all criteria
/// are returned (used by the cross-module facade).
/// </summary>
internal sealed record GetRegistrationsQuery(
    TicketedEventId EventId,
    TeamId TeamId,
    QueryRegistrationsDto? Filter = null)
    : Query<IReadOnlyList<RegistrationListItemDto>?>;
