using Amolenk.Admitto.Module.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Module.Shared.Application.Messaging;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Module.Registrations.Application.UseCases.Registrations.GetRegistrationDetails;

internal sealed record GetRegistrationDetailsQuery(
    Guid TeamId,
    TicketedEventId EventId,
    RegistrationId RegistrationId) : Query<RegistrationDetailDto?>;
