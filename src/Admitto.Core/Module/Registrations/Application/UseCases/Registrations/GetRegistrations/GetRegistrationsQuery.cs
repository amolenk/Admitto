using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Core.Module.Registrations.Application.UseCases.Registrations.GetRegistrations;

internal sealed record GetRegistrationsQuery(TeamId TeamId, TicketedEventId EventId)
    : Query<IReadOnlyList<RegistrationListItemDto>?>;
