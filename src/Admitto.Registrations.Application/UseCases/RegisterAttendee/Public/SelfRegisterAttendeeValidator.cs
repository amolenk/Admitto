// using Amolenk.Admitto.Registrations.Domain.ValueObjects;
// using Amolenk.Admitto.Shared.Application.Validation;
// using Amolenk.Admitto.Shared.Kernel.ValueObjects;
// using FluentValidation;
//
// namespace Amolenk.Admitto.Registrations.Application.UseCases.RegisterAttendee.Public;
//
// public class SelfRegisterAttendeeValidator : AbstractValidator<SelfRegisterAttendeeHttpRequest>
// {
//     public SelfRegisterAttendeeValidator()
//     {
//         RuleFor(x => x.Email)
//             .MustBeParseable(EmailAddress.TryFrom);
//
//         RuleForEach(x => x.TicketTypeSlugs)
//             .MustBeParseable(TicketTypeId.TryFrom);
//     }
// }
