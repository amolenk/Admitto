using Amolenk.Admitto.Application.Abstractions;
using Amolenk.Admitto.Domain.Entities;
using MediatR;

namespace Amolenk.Admitto.Infrastructure.Persistence;

public class TicketedEventRepository(IMediator mediator)
    : AggregateRepositoryBase<TicketedEvent>(mediator), ITicketedEventRepository
{
}
