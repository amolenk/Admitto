using Amolenk.Admitto.Organization.Contracts;
using Amolenk.Admitto.Shared.Application.Messaging;
using Amolenk.Admitto.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Organization.Application.UseCases.GetTicketTypes;

internal record GetTicketTypesQuery(TicketedEventId TicketedEventId) : Query<TicketTypeDto[]>;