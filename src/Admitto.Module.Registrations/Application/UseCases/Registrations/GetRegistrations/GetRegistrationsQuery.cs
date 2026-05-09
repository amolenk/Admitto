using Amolenk.Admitto.Module.Shared.Application.Messaging;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Module.Registrations.Application.UseCases.Registrations.GetRegistrations;

internal sealed record GetRegistrationsQuery(TeamId TeamId, TicketedEventId EventId)
    : Query<IReadOnlyList<RegistrationListItemDto>?>;
