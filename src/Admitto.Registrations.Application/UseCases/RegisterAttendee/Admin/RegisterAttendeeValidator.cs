// using Amolenk.Admitto.Registrations.Domain.ValueObjects;
// using Amolenk.Admitto.Shared.Application.Validation;
// using Amolenk.Admitto.Shared.Kernel.ValueObjects;
// using FluentValidation;
//
// namespace Amolenk.Admitto.Registrations.Application.UseCases.RegisterAttendee.Admin;
//
// public class RegisterAttendeeValidator : AbstractValidator<RegisterAttendeeHttpRequest>
// {
//     public RegisterAttendeeValidator()
//     {
//         RuleFor(x => x.Email)
//             .MustBeParseable(EmailAddress.TryFrom);
//
//         RuleFor(x => x.AttendeeInfo)
//             .NotNull()
//             .ChildRules(ai =>
//             {
//                 ai.RuleFor(x => x.FirstName)
//                     .MustBeParseable(FirstName.TryFrom);
//                 
//                 ai.RuleFor(x => x.LastName)
//                     .MustBeParseable(LastName.TryFrom);
//             });
//
//         RuleFor(x => x.TicketRequests)
//             .NotNull()
//             .NotEmpty()
//             .ForEach(x => x
//                 .NotNull()
//                 .ChildRules(tr =>
//                 {
//                     tr.RuleFor(r => r.TicketTypeId)
//                         .MustBeParseable(TicketTypeId.TryFrom);
//                 }));
//     }
// }