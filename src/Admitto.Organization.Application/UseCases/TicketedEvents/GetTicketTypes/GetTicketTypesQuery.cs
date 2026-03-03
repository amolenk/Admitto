using Amolenk.Admitto.Organization.Contracts;
using Amolenk.Admitto.Shared.Application.Messaging;
using Amolenk.Admitto.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Organization.Application.UseCases.TicketedEvents.GetTicketTypes;

internal record GetTicketTypesQuery(Guid TicketedEventId) : Query<TicketTypeDto[]>;