// using Amolenk.Admitto.Module.Organization.Contracts;
// using Amolenk.Admitto.Module.Registrations.Application.Mapping;
// using Amolenk.Admitto.Module.Registrations.Application.Persistence;
// using Amolenk.Admitto.Module.Registrations.Application.Services;
// using Amolenk.Admitto.Module.Registrations.Domain.Entities;
// using Amolenk.Admitto.Module.Registrations.Domain.ValueObjects;
// using Amolenk.Admitto.Module.Shared.Application.Messaging;
//
// namespace Amolenk.Admitto.Module.Registrations.Application.UseCases.RegisterAttendee;
//
// internal class RegisterAttendeeHandler(
//     ICapacityTracker capacityTracker,
//     IOrganizationFacade organizationFacade,
//     IRegistrationsWriteStore writeStore)
//     : ICommandHandler<RegisterAttendeeCommand, RegistrationId>
// {
//     public async ValueTask<RegistrationId> HandleAsync(
//         RegisterAttendeeCommand command,
//         CancellationToken cancellationToken)
//     {
//         // Get ticket types once to make sure they're consistent and deterministic for the duration of the handler.
//         var ticketTypes = (await organizationFacade
//                 .GetTicketTypesAsync(command.EventId.Value, cancellationToken))
//             .Select(ticketType => ticketType.ToDomain())
//             .ToList();
//         
//         // Create the registration.
//         var registration = Registration.Create(
//             command.EventId,
//             command.EmailAddress,
//             command.AttendeeInfo);
//
//         // Grant the tickets without worrying about capacity for now.
//         // There may be other validation errors to catch first.
//         var tickets = registration.GrantTickets(
//             command.TicketRequests,
//             ticketTypes);
//         
//         // Minimize contention by checking for capacity only after we know that the registration is valid.
//         await capacityTracker.ClaimTicketsAsync(
//             command.EventId,
//             tickets,
//             cancellationToken);
//
//         // Persist the new registration.
//         writeStore.Registrations.SaveAggregate(registration);
//
//         return registration.Id;
//     }
// }