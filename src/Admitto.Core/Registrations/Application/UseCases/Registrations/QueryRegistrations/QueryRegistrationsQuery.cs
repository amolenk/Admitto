using Amolenk.Admitto.Core.Registrations.Contracts;
using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.Registrations.QueryRegistrations;

internal sealed record QueryRegistrationsQuery(TicketedEventId EventId, QueryRegistrationsDto Filter)
    : Query<IReadOnlyList<RegistrationListItemDto>>;
