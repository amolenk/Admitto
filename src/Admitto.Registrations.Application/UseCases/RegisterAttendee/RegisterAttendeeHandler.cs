using Amolenk.Admitto.Organization.Contracts;
using Amolenk.Admitto.Registrations.Application.Mapping;
using Amolenk.Admitto.Registrations.Application.Persistence;
using Amolenk.Admitto.Registrations.Application.Services;
using Amolenk.Admitto.Registrations.Domain.Entities;
using Amolenk.Admitto.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Shared.Application.Messaging;

namespace Amolenk.Admitto.Registrations.Application.UseCases.RegisterAttendee;

internal class RegisterAttendeeHandler(
    ICapacityTracker capacityTracker,
    IOrganizationFacade organizationFacade,
    IRegistrationsWriteStore writeStore)
    : ICommandHandler<RegisterAttendeeCommand, RegistrationId>
{
    public async ValueTask<RegistrationId> HandleAsync(
        RegisterAttendeeCommand command,
        CancellationToken cancellationToken)
    {
        // Get ticket types once to make sure ticket types are
        // consistent and deterministic for the duration of the handler.
        var ticketTypes = (await organizationFacade
                .GetTicketTypesAsync(command.EventId.Value, cancellationToken))
            .Select(ticketType => ticketType.ToDomain())
            .ToList();
        
        // Create the registration.
        var registration = Registration.Create(
            command.EventId,
            command.EmailAddress);

        // Let's be optimistic and assume there's enough capacity.	
        registration.GrantTickets(
            command.TicketRequests,
            ticketTypes);
        
        // Minimize contention by checking for capacity only after we
        // know that the registration is valid.
        await capacityTracker.ClaimTicketsAsync(
            command.EventId,
            command.TicketRequests,
            cancellationToken);

        // Persist the new registration.
        writeStore.Registrations.Add(registration);

        return registration.Id;
    }
}