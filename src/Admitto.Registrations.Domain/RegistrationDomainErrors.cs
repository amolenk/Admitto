// using Amolenk.Admitto.Shared.Kernel.ErrorHandling;
//
// namespace Amolenk.Admitto.Registrations.Domain;
//
// internal static class RegistrationDomainErrors
// {
//     public static Error UnknownTicketTypes(TicketTypeId[] ids) =>
//         new(
//             "reg.ticket_types_unknown",
//             "One or more ticket types are unknown.",
//             ErrorType.Validation,
//             new Dictionary<string, object?> { ["ticketTypeIds"] = ids });
//
//     public static Error DuplicateTicketTypes(TicketTypeId[] ids) =>
//         new(
//             "reg.duplicate_ticket_types",
//             "One or more ticket types are duplicates.",
//             ErrorType.Validation,
//             new Dictionary<string, object?> { ["ticketTypeIds"] = ids });
//
//     public static Error TicketTypeAlreadyGranted(TicketTypeId id) =>
//         new(
//             "reg.duplicate_ticket_type",
//             "The same ticket type has already been granted.",
//             ErrorType.Validation,
//             new Dictionary<string, object?> { ["ticketTypeId"] = id });
//     
//     public static Error OverlappingTicketTypeTimeSlots(TicketTypeId[] ids) =>
//         new(
//             "reg.overlapping_ticket_type_time_slots",
//             "One or more ticket types have overlapping time slots.",
//             ErrorType.Validation,
//             new Dictionary<string, object?> { ["ticketTypeIds"] = ids });
// }