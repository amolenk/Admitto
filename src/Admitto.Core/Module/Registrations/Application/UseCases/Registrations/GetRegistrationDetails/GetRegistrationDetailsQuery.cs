using Amolenk.Admitto.Core.Module.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Core.Module.Registrations.Application.UseCases.Registrations.GetRegistrationDetails;

internal sealed record GetRegistrationDetailsQuery(
    Guid TeamId,
    TicketedEventId EventId,
    RegistrationId RegistrationId) : Query<RegistrationDetailDto?>;
