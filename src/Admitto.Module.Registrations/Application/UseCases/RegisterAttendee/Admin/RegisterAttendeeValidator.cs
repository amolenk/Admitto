// using Amolenk.Admitto.Module.Registrations.Domain.ValueObjects;
// using Amolenk.Admitto.Module.Shared.Application.Validation;
// using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;
// using FluentValidation;
//
// namespace Amolenk.Admitto.Module.Registrations.Application.UseCases.RegisterAttendee.Admin;
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