// using Amolenk.Admitto.Module.Registrations.Domain.ValueObjects;
// using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;
//
// namespace Amolenk.Admitto.Module.Registrations.Application.UseCases.RegisterAttendee.Admin;
//
// public record RegisterAttendeeHttpRequest(
//     string Email,
//     AttendeeInfoDto AttendeeInfo,
//     TicketRequestDto[] TicketRequests)
// {
//     internal RegisterAttendeeCommand ToCommand(TicketedEventId eventId)
//         => new(
//             eventId,
//             EmailAddress.From(Email),
//             AttendeeInfo.ToDomain(),
//             TicketRequests.Select(t => t.ToDomain()).ToArray());
// }
//
// public record AttendeeInfoDto(string FirstName, string LastName)
// {
//     internal AttendeeInfo ToDomain()
//         => new(
//             Domain.ValueObjects.FirstName.From(FirstName),
//             Domain.ValueObjects.LastName.From(LastName));
// }
//
// public record TicketRequestDto(Guid TicketTypeId)
// {
//     internal TicketRequest ToDomain()
//         => new(
//             Module.Shared.Kernel.ValueObjects.TicketTypeId.From(TicketTypeId),
//             TicketGrantMode.Privileged,
//             CapacityEnforcementMode.Ignore);
// }