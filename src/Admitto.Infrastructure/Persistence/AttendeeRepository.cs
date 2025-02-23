using Amolenk.Admitto.Application.Abstractions;
using Amolenk.Admitto.Application.Dtos;
using Amolenk.Admitto.Domain.Entities;
using MediatR;

namespace Amolenk.Admitto.Infrastructure.Persistence;

public class AttendeeRepository(IMediator mediator)
    : AggregateRepositoryBase<Attendee>(mediator)
    , IAttendeeRepository
{
}
