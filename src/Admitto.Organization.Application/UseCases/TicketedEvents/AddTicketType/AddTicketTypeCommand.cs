using Amolenk.Admitto.Shared.Application.Messaging;
using Amolenk.Admitto.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Organization.Application.UseCases.AddTicketType;

internal record AddTicketTypeCommand(TicketedEventId EventId) : Command;