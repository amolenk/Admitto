using Amolenk.Admitto.Module.Shared.Application.Messaging;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Module.Registrations.Application.UseCases.RegistrationPolicy.OpenRegistration;

internal sealed record OpenRegistrationCommand(TicketedEventId EventId) : Command;
