using Amolenk.Admitto.Module.Registrations.Contracts;
using Amolenk.Admitto.Module.Shared.Application.Messaging;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Module.Registrations.Application.UseCases.Registrations.QueryRegistrations;

internal sealed record QueryRegistrationsQuery(TicketedEventId EventId, QueryRegistrationsDto Filter)
    : Query<IReadOnlyList<RegistrationListItemDto>>;
