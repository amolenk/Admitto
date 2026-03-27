using Amolenk.Admitto.Module.Organization.Contracts;
using Amolenk.Admitto.Module.Shared.Application.Messaging;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Module.Organization.Application.UseCases.TicketedEvents.GetTicketTypes;

internal record GetTicketTypesQuery(Guid TicketedEventId) : Query<TicketTypeDto[]>;