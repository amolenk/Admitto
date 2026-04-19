using Amolenk.Admitto.Module.Shared.Application.Messaging;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Module.Registrations.Application.UseCases.RegistrationPolicy.GetRegistrationOpenStatus;

internal sealed record GetRegistrationOpenStatusQuery(TicketedEventId EventId)
    : Query<RegistrationOpenStatusDto>;
